using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PoFunQuiz.Web.Middleware;
using System.Text.Json;
using Xunit;

namespace PoFunQuiz.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="GlobalExceptionHandlerMiddleware"/>.
/// Validates that unhandled exceptions are caught, logged, and serialised
/// as a structured 500 JSON response — never leaking stack traces outside Development.
/// </summary>
public class GlobalExceptionHandlerMiddlewareTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IWebHostEnvironment MakeEnv(string name)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(name);
        return env.Object;
    }

    private static GlobalExceptionHandlerMiddleware BuildMiddleware(
        RequestDelegate next,
        string environment = "Development")
    {
        return new GlobalExceptionHandlerMiddleware(
            next,
            NullLogger<GlobalExceptionHandlerMiddleware>.Instance,
            MakeEnv(environment));
    }

    private static async Task<(HttpContext ctx, string body)> InvokeAsync(
        GlobalExceptionHandlerMiddleware mw)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        await mw.InvokeAsync(ctx);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        return (ctx, body);
    }

    // ── Status code ───────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenExceptionThrown_Returns500StatusCode()
    {
        var mw = BuildMiddleware(_ => throw new InvalidOperationException("boom"));

        var (ctx, _) = await InvokeAsync(mw);

        ctx.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task WhenNoException_PassesThrough_WithOriginalStatus()
    {
        var mw = BuildMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        var (ctx, _) = await InvokeAsync(mw);

        ctx.Response.StatusCode.Should().Be(200);
    }

    // ── Response body JSON shape ──────────────────────────────────────────────

    [Fact]
    public async Task WhenExceptionThrown_ResponseBody_IsValidJson()
    {
        var mw = BuildMiddleware(_ => throw new InvalidOperationException("json-test"));

        var (_, body) = await InvokeAsync(mw);

        var act = () => JsonDocument.Parse(body);
        act.Should().NotThrow("response body must be parseable JSON");
    }

    [Fact]
    public async Task WhenExceptionThrown_ResponseBody_ContainsStatusCode500()
    {
        var mw = BuildMiddleware(_ => throw new InvalidOperationException("code-test"));

        var (_, body) = await InvokeAsync(mw);

        // The middleware serialises with JsonSerializer.Serialize() (no camelCase option),
        // so properties appear in their declared casing: "StatusCode", "Message", etc.
        var doc = JsonDocument.Parse(body);
        // Accept either "StatusCode" (default casing) or "statusCode" (configured camelCase)
        var found = doc.RootElement.TryGetProperty("StatusCode", out var statusProp)
                 || doc.RootElement.TryGetProperty("statusCode", out statusProp);
        found.Should().BeTrue("response must contain a StatusCode field");
        statusProp.GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task InDevelopment_ResponseBody_ContainsExceptionMessage()
    {
        var mw = BuildMiddleware(
            _ => throw new InvalidOperationException("secret detail"), "Development");

        var (_, body) = await InvokeAsync(mw);

        body.Should().Contain("secret detail");
    }

    [Fact]
    public async Task InProduction_ResponseBody_DoesNotContainExceptionMessage()
    {
        var mw = BuildMiddleware(
            _ => throw new InvalidOperationException("secret internal detail"), "Production");

        var (_, body) = await InvokeAsync(mw);

        // Production should serve a generic message, never the raw exception text
        body.Should().NotContain("secret internal detail");
        body.Should().Contain("internal server error");
    }

    // ── Content-Type ─────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenExceptionThrown_ContentType_IsApplicationJson()
    {
        var mw = BuildMiddleware(_ => throw new InvalidOperationException("content-type-test"));

        var (ctx, _) = await InvokeAsync(mw);

        ctx.Response.ContentType.Should().Contain("application/json");
    }

    // ── FileNotFoundException → 404 ──────────────────────────────────────────

    [Fact]
    public async Task WhenFileNotFoundExceptionThrown_Returns404()
    {
        var mw = BuildMiddleware(_ => throw new FileNotFoundException("missing.css"));

        var (ctx, _) = await InvokeAsync(mw);

        ctx.Response.StatusCode.Should().Be(404);
    }
}
