using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;

namespace PoFunQuiz.Web.HealthChecks;

public class TableStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TableStorageHealthCheck> _logger;

    public TableStorageHealthCheck(
        IConfiguration configuration,
        ILogger<TableStorageHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("tables")
                ?? _configuration["AppSettings:Storage:TableStorageConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Degraded("Table Storage connection string is not configured");
            }

            var serviceClient = new TableServiceClient(connectionString);
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
