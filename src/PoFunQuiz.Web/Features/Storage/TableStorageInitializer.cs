using Azure.Data.Tables;

namespace PoFunQuiz.Web.Features.Storage;

/// <summary>
/// Hosted service that initializes Azure Table Storage tables at application startup.
/// </summary>
public class TableStorageInitializer : IHostedService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<TableStorageInitializer> _logger;

    public TableStorageInitializer(
        TableServiceClient tableServiceClient,
        ILogger<TableStorageInitializer> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Azure Table Storage tables...");

            await _tableServiceClient.CreateTableIfNotExistsAsync("GameSessions", cancellationToken);
            _logger.LogInformation("Ensured 'GameSessions' table exists");

            await _tableServiceClient.CreateTableIfNotExistsAsync("PoFunQuizPlayers", cancellationToken);
            _logger.LogInformation("Ensured 'PoFunQuizPlayers' table exists");

            _logger.LogInformation("Azure Table Storage initialization complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Table Storage. The application may not function correctly.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TableStorageInitializer stopping");
        return Task.CompletedTask;
    }
}
