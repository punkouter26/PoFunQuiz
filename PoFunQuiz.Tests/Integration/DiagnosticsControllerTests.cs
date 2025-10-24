using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

public class DiagnosticsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DiagnosticsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DiagnosticsHealth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DiagnosticsHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("healthy", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiagnosticsHealth_ReturnsJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/health");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TestOpenAIConnection_ReturnsSuccessOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/openai");

        // Assert - Can be either success (if OpenAI is configured) or service unavailable
        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected success or 503, but got {response.StatusCode}");
    }

    [Fact]
    public async Task TestTableStorageConnection_ReturnsSuccessOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/tablestorage");

        // Assert
        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected success or 503, but got {response.StatusCode}");
    }

    [Fact]
    public async Task TestOpenAIConnection_ContainsConnectionStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/diagnostics/openai");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(
            content.Contains("success", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("error", StringComparison.OrdinalIgnoreCase),
            "Response should indicate connection status");
    }

    [Fact]
    public async Task DiagnosticsEndpoints_ReturnWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var healthResponse = await _client.GetAsync("/api/diagnostics/health", cts.Token);
        var openAIResponse = await _client.GetAsync("/api/diagnostics/openai", cts.Token);
        var storageResponse = await _client.GetAsync("/api/diagnostics/tablestorage", cts.Token);

        // Assert
        Assert.NotNull(healthResponse);
        Assert.NotNull(openAIResponse);
        Assert.NotNull(storageResponse);
    }
}
