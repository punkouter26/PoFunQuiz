using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using PoFunQuiz.Web.Configuration;

namespace PoFunQuiz.Web.HealthChecks;

public class OpenAIHealthCheck : IHealthCheck
{
    private readonly IOptions<OpenAISettings> _openAISettings;
    private readonly ILogger<OpenAIHealthCheck> _logger;

    public OpenAIHealthCheck(
        IOptions<OpenAISettings> openAISettings,
        ILogger<OpenAIHealthCheck> logger)
    {
        _openAISettings = openAISettings;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _openAISettings.Value;

            // Simple check to verify configuration is complete
            if (string.IsNullOrEmpty(settings.Endpoint) ||
                string.IsNullOrEmpty(settings.ApiKey) ||
                string.IsNullOrEmpty(settings.DeploymentName))
            {
                return Task.FromResult(HealthCheckResult.Degraded("OpenAI configuration is incomplete"));
            }

            var client = new AzureOpenAIClient(
                new Uri(settings.Endpoint),
                new Azure.AzureKeyCredential(settings.ApiKey));

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
