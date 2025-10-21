using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;

namespace PoFunQuiz.Server.HealthChecks;

public class TableStorageHealthCheck : IHealthCheck
{
    private readonly StorageSettings _settings;
    private readonly ILogger<TableStorageHealthCheck> _logger;

    public TableStorageHealthCheck(
        IOptions<AppSettings> appSettings,
        ILogger<TableStorageHealthCheck> logger)
    {
        _settings = appSettings.Value.Storage;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceClient = new TableServiceClient(_settings.TableStorageConnectionString);
            await serviceClient.GetPropertiesAsync(cancellationToken);

            return HealthCheckResult.Healthy("Table Storage is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Storage health check failed");
            return HealthCheckResult.Unhealthy(
                "Table Storage is not accessible",
                exception: ex);
        }
    }
}
