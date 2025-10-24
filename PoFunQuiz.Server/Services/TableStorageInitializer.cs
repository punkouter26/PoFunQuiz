using Azure.Data.Tables;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PoFunQuiz.Server.Services
{
    /// <summary>
    /// Hosted service that initializes Azure Table Storage tables at application startup.
    /// This follows best practices by moving infrastructure initialization out of service constructors.
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

                // Create GameSessions table
                await _tableServiceClient.CreateTableIfNotExistsAsync("GameSessions", cancellationToken);
                _logger.LogInformation("Ensured 'GameSessions' table exists");

                // Create Players table (managed by PlayerStorageService but we ensure it here too)
                await _tableServiceClient.CreateTableIfNotExistsAsync("PoFunQuizPlayers", cancellationToken);
                _logger.LogInformation("Ensured 'PoFunQuizPlayers' table exists");

                _logger.LogInformation("Azure Table Storage initialization complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Table Storage. The application may not function correctly.");
                // Don't throw - allow app to start even if table creation fails
                // Services will handle missing tables appropriately
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TableStorageInitializer stopping");
            return Task.CompletedTask;
        }
    }
}
