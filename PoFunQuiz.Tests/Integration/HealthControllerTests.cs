using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_ReturnsHealthCheckStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("status", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checks", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_ContainsTableStorageCheck()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("table_storage", content);
    }

    [Fact]
    public async Task Health_ContainsOpenAICheck()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("openai", content);
    }

    [Fact]
    public async Task Health_ContainsInternetCheck()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("internet", content);
    }

    [Fact]
    public async Task Health_ReturnsWithinReasonableTime()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10);
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.GetAsync("/api/health", cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(duration < timeout, $"Health check took {duration.TotalSeconds}s, expected less than {timeout.TotalSeconds}s");
    }
}
