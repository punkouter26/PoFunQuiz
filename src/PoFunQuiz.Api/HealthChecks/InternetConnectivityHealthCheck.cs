using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PoFunQuiz.Server.HealthChecks;

public class InternetConnectivityHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InternetConnectivityHealthCheck> _logger;

    public InternetConnectivityHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<InternetConnectivityHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                "https://www.microsoft.com",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Internet connectivity is available");
            }

            return HealthCheckResult.Degraded(
                $"Internet connectivity check returned status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Internet connectivity health check failed");
            return HealthCheckResult.Unhealthy(
                "No internet connectivity",
                exception: ex);
        }
    }
}
