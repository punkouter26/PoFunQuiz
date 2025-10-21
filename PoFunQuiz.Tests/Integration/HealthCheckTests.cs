using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            "Health endpoint should return 200 or 503");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsValidJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.NotEmpty(content);
        Assert.Contains("status", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthEndpoint_ContainsExpectedHealthChecks()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("table_storage", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("openai", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internet", content, StringComparison.OrdinalIgnoreCase);
    }
}
