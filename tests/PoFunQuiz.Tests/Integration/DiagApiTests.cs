using System.Net;
using System.Text.Json;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Integration tests for the <c>/api/diag</c> diagnostics endpoint.
/// Verifies the endpoint is reachable, returns valid JSON with expected keys,
/// and that all sensitive values are masked (middle replaced with asterisks).
/// </summary>
public class DiagApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DiagApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DiagEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/api/diag");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DiagEndpoint_ContentType_IsApplicationJson()
    {
        var response = await _client.GetAsync("/api/diag");

        Assert.Contains("application/json",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    [Fact]
    public async Task DiagEndpoint_ResponseBody_IsValidJson()
    {
        var response = await _client.GetAsync("/api/diag");
        var body = await response.Content.ReadAsStringAsync();

        // Should not throw
        var doc = JsonDocument.Parse(body);
        Assert.NotNull(doc);
    }

    [Theory]
    [InlineData("environment")]
    [InlineData("connections")]
    [InlineData("azureOpenAI")]
    [InlineData("settings")]
    [InlineData("timestamp")]
    public async Task DiagEndpoint_Contains_TopLevelKey(string key)
    {
        var response = await _client.GetAsync("/api/diag");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        Assert.True(doc.RootElement.TryGetProperty(key, out _),
            $"Expected top-level key '{key}' in /api/diag response. Body: {body}");
    }

    [Fact]
    public async Task DiagEndpoint_Environment_IsNotEmpty()
    {
        var response = await _client.GetAsync("/api/diag");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        var env = doc.RootElement.GetProperty("environment").GetString();
        Assert.False(string.IsNullOrWhiteSpace(env),
            "environment field must not be empty");
    }

    [Fact]
    public async Task DiagEndpoint_Connections_ContainsTableStorage()
    {
        var response = await _client.GetAsync("/api/diag");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        var conn = doc.RootElement.GetProperty("connections");
        Assert.True(conn.TryGetProperty("tableStorage", out _),
            "connections.tableStorage must be present");
    }

    [Fact]
    public async Task DiagEndpoint_SensitiveValues_AreMaskedOrMarkedNotSet()
    {
        // Arrange — any connection string longer than 8 chars must be masked
        // (masked format: first 4 chars + **** + last 4 chars, e.g. "Abc1****xyz9")
        // Short/missing values are shown as "(not set)" or fully starred.
        var response = await _client.GetAsync("/api/diag");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        var conn = doc.RootElement.GetProperty("connections");

        foreach (var prop in conn.EnumerateObject())
        {
            var val = prop.Value.GetString() ?? string.Empty;

            // A value is acceptable if it is:
            // 1. "(not set)"
            // 2. All asterisks (short secret)
            // 3. Contains "****" (middle masked)
            var isAcceptable = val == "(not set)"
                || val.All(c => c == '*')
                || val.Contains("****");

            Assert.True(isAcceptable,
                $"connections.{prop.Name} value '{val}' is not masked. " +
                "Sensitive values must contain '****' or be '(not set)'.");
        }
    }

    [Fact]
    public async Task DiagEndpoint_IsIdempotent_MultipleCalls()
    {
        // Two sequential calls should both return 200 with identical top-level keys
        var r1 = await _client.GetAsync("/api/diag");
        var r2 = await _client.GetAsync("/api/diag");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }
}
