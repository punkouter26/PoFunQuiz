using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using PoFunQuiz.Web.HealthChecks;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Tests that <see cref="TableStorageHealthCheck"/> correctly reports <c>Unhealthy</c>
/// when Azure Table Storage is unreachable, and <c>Degraded</c> when not configured.
/// These tests instantiate the health check directly with controlled configuration,
/// faster and more reliable than going through the full HTTP pipeline.
/// </summary>
public class TableStorageHealthCheckUnhealthyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration MakeConfig(string? connectionString)
    {
        var dict = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(connectionString))
            dict["ConnectionStrings:tables"] = connectionString;
        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private static TableStorageHealthCheck MakeCheck(string? connectionString) =>
        new(MakeConfig(connectionString),
            NullLogger<TableStorageHealthCheck>.Instance);

    private static HealthCheckContext MakeContext(IHealthCheck check) =>
        new()
        {
            Registration = new HealthCheckRegistration("table_storage", check, null, null)
        };

    // ── Missing / empty config ────────────────────────────────────────────────

    [Fact]
    public async Task WhenConnectionStringMissing_ReturnsDegraded()
    {
        var check = MakeCheck(null);

        var result = await check.CheckHealthAsync(MakeContext(check));

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task WhenConnectionStringMissing_DescriptionMentionsMissingConfig()
    {
        var check = MakeCheck(null);

        var result = await check.CheckHealthAsync(MakeContext(check));

        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.Contains("connection string", result.Description,
            StringComparison.OrdinalIgnoreCase);
    }

    // ── Unreachable endpoint ──────────────────────────────────────────────────

    [Fact]
    public async Task WhenConnectionStringIsUnreachable_ReturnsUnhealthy()
    {
        // Syntactically-valid connection string pointing to a non-existent host;
        // GetPropertiesAsync() will throw a network exception.
        const string brokenConn =
            "DefaultEndpointsProtocol=https;AccountName=invalidacct0;AccountKey=" +
            "dGVzdGtleXRlc3RrZXl0ZXN0a2V5dGVzdGtleXRlc3Q=;EndpointSuffix=invalid.local";

        var check = MakeCheck(brokenConn);

        var result = await check.CheckHealthAsync(
            MakeContext(check), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task WhenConnectionStringIsUnreachable_DescriptionIndicatesInaccessible()
    {
        const string brokenConn =
            "DefaultEndpointsProtocol=https;AccountName=invalidacct1;AccountKey=" +
            "dGVzdGtleXRlc3RrZXl0ZXN0a2V5dGVzdGtleXRlc3Q=;EndpointSuffix=invalid.local";

        var check = MakeCheck(brokenConn);

        var result = await check.CheckHealthAsync(MakeContext(check));

        Assert.Contains("not accessible", result.Description,
            StringComparison.OrdinalIgnoreCase);
    }
}
