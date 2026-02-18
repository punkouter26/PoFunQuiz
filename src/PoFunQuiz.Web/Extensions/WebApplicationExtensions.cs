using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Serilog;
using System.Text.Json;

namespace PoFunQuiz.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> that keep Program.cs focused on
/// wiring order rather than implementation details.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Appends a Content-Security-Policy header to every response.
    /// Permits Google Fonts, self-hosted scripts/styles, and Blazor SignalR WebSocket connections.
    /// </summary>
    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder app, bool isDevelopment = false)
    {
        app.Use(async (context, next) =>
        {
            // In development, also allow ws: so the hot-reload WebSocket is not blocked by CSP
            var connectSrc = isDevelopment ? "connect-src 'self' ws: wss:;" : "connect-src 'self' wss:;";
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline'; " +      // Blazor requires inline scripts
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data:; " +
                connectSrc);                                  // SignalR + dev hot-reload WebSocket
            await next();
        });
        return app;
    }

    /// <summary>
    /// Registers ApplicationStarted/Stopping/Stopped lifetime callbacks that log bound
    /// addresses and state transitions via Serilog.
    /// </summary>
    public static WebApplication UseLifetimeLogging(this WebApplication app)
    {
        var lifetime = app.Lifetime;

        lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = app.Services
                .GetService<IServer>()
                ?.Features.Get<IServerAddressesFeature>()?.Addresses;

            if (addresses != null)
            {
                foreach (var address in addresses)
                    Log.Information("Now listening on: {Address}", address);
            }

            Log.Information("Application started");
        });

        lifetime.ApplicationStopping.Register(() => Log.Information("Application is stopping"));
        lifetime.ApplicationStopped.Register(() => Log.Information("Application stopped"));

        return app;
    }

    /// <summary>
    /// Maps <c>/health</c> with a structured JSON response writer showing every check's
    /// status, duration, and any exception message.
    /// </summary>
    public static IEndpointRouteBuilder MapJsonHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        exception = e.Value.Exception?.Message
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });
        return endpoints;
    }
}
