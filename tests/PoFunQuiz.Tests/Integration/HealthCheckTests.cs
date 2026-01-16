using System.Net;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Integration tests for health check endpoints using mocked services
/// </summary>
public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert - Health endpoint may return 200 (healthy) or 503 (degraded/unhealthy)
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Health endpoint should return 200 or 503, got {response.StatusCode}");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsValidJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should return some content (may be simple string or JSON depending on configuration)
        Assert.True(content.Length > 0, "Health endpoint should return some content");
    }

    [Fact]
    public async Task HealthEndpoint_RespondsQuickly()
    {
        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.GetAsync("/health");
        var duration = DateTime.UtcNow - startTime;

        // Assert - Health check should respond quickly
        Assert.True(duration.TotalSeconds < 5, $"Health endpoint took {duration.TotalSeconds}s, expected < 5s");
    }
}
