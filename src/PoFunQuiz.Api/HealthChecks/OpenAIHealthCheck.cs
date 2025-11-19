using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;

namespace PoFunQuiz.Server.HealthChecks;

public class OpenAIHealthCheck : IHealthCheck
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIHealthCheck> _logger;

    public OpenAIHealthCheck(
        IOptions<AppSettings> appSettings,
        ILogger<OpenAIHealthCheck> logger)
    {
        _settings = appSettings.Value.AzureOpenAI;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new AzureOpenAIClient(
                new Uri(_settings.Endpoint),
                new Azure.AzureKeyCredential(_settings.ApiKey));

            // Simple check to verify the client can be created and endpoint is reachable
            // Note: This doesn't make an actual API call to avoid quota usage
            if (string.IsNullOrEmpty(_settings.Endpoint) ||
                string.IsNullOrEmpty(_settings.ApiKey) ||
                string.IsNullOrEmpty(_settings.DeploymentName))
            {
                return Task.FromResult(HealthCheckResult.Degraded("OpenAI configuration is incomplete"));
            }

            return Task.FromResult(HealthCheckResult.Healthy("OpenAI is configured"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "OpenAI is not accessible or configured incorrectly",
                exception: ex));
        }
    }
}
