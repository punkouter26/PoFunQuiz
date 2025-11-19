using System;
using System.Collections.Generic;
using System.Linq; // Added for LINQ operations like Take, Select
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using Azure.Data.Tables; // Added for Table Storage
using PoFunQuiz.Infrastructure.Storage; // Added for GameSessionEntity

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that manages game sessions and results using Azure Table Storage.
    /// </summary>
    public class GameSessionService : IGameSessionService
    {
        private readonly IPlayerStorageService _playerStorageService;
        private readonly IScoringService _scoringService;
        private readonly ILogger<GameSessionService> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "GameSessions";

        public GameSessionService(
            IPlayerStorageService playerStorageService,
            IScoringService scoringService,
            ILogger<GameSessionService> logger,
            TableServiceClient tableServiceClient)
        {
            _playerStorageService = playerStorageService;
            _scoringService = scoringService;
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            // Table initialization is now handled by TableStorageInitializer IHostedService
        }

        /// <inheritdoc />
        public async Task<GameSession> CreateGameSessionAsync(Player player1, Player player2) // Made async
        {
            if (player1 == null)
                throw new ArgumentNullException(nameof(player1));

            if (player2 == null)
                throw new ArgumentNullException(nameof(player2));

            try
            {
                // Create new game session model
                var gameSession = new GameSession
                {
                    GameId = Guid.NewGuid().ToString(),
                    Player1 = player1, // Keep Player objects in the model for this instance
                    Player2 = player2, // Keep Player objects in the model for this instance
                    Player1Initials = player1.Initials, // Ensure initials are set
                    Player2Initials = player2.Initials, // Ensure initials are set
                    StartTime = DateTime.UtcNow
                };

                // Get table client and add entity
                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var entity = GameSessionEntity.FromModel(gameSession); // Convert model to entity for storage
                await tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Created and saved GameSession {GameId} to table storage.", gameSession.GameId);

                return gameSession; // Return the original model
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game session for players {Player1} and {Player2} in table storage",
                   player1.Initials, player2.Initials);
                throw; // Rethrow to indicate failure
            }
        }

        /// <inheritdoc />
        public async Task<GameSession> SaveGameResultsAsync(GameSession gameSession)
        {
            ValidateGameSession(gameSession);

            try
            {
                FinalizeGameSession(gameSession);
                var gameResult = _scoringService.DetermineGameResult(gameSession);
                await UpdatePlayerStatistics(gameSession, gameResult);
                await PersistGameSession(gameSession);

                return gameSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving game session results for game {GameId} to table storage", gameSession.GameId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<GameSession>> GetRecentGameSessionsAsync(string? playerInitials = default, int count = 10) // Made async
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var sessionEntities = new List<GameSessionEntity>();
                string? filter = null;

                // Filter by player, if specified
                if (!string.IsNullOrWhiteSpace(playerInitials))
                {
                    string formattedInitials = playerInitials.ToUpperInvariant();
                    // Build a filter query for Table Storage
                    // Note: Table Storage queries have limitations. Complex OR queries might be inefficient.
                    // Consider querying twice or adjusting partition strategy if performance is critical.
                    filter = $"Player1Initials eq '{formattedInitials}' or Player2Initials eq '{formattedInitials}'";
                    _logger.LogInformation("Applying filter for player initials: {Filter}", filter);
                }
                else
                {
                    _logger.LogInformation("No player filter applied, fetching general recent games.");
                }

                // Query the table
                // Fetching more items than 'count' to allow for in-memory sorting by EndTime
                _logger.LogInformation("Querying Azure Table '{TableName}'...", TableName);
                await foreach (var entity in tableClient.QueryAsync<GameSessionEntity>(filter: filter, maxPerPage: count * 2))
                {
                    sessionEntities.Add(entity);
                }
                _logger.LogInformation("Initial query returned {EntityCount} entities.", sessionEntities.Count);

                // Sort in memory (Table Storage sorting is limited, especially on nullable DateTimeOffset)
                sessionEntities.Sort((a, b) => Nullable.Compare(b.EndTime, a.EndTime));

                // Take the required count and convert back to model
                // Note: Player objects are not populated here, only initials.
                // The calling code (e.g., Leaderboard) needs to handle fetching full Player details if required.
                var recentSessions = sessionEntities
                    .Take(count)
                    .Select(entity => entity.ToModel()) // Pass null for players
                    .ToList();

                _logger.LogInformation("Retrieved {Count} recent game sessions from table storage after sorting and taking count.", recentSessions.Count);

                return recentSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent game sessions from table storage");
                // Return empty list on error to avoid crashing leaderboard, but log the issue
                return new List<GameSession>();
                // throw; // Optionally rethrow if failure should halt operation
            }
        }

        private void ValidateGameSession(GameSession gameSession)
        {
            if (gameSession == null)
                throw new ArgumentNullException(nameof(gameSession));
        }

        private void FinalizeGameSession(GameSession gameSession)
        {
            if (!gameSession.EndTime.HasValue)
            {
                gameSession.EndTime = DateTime.UtcNow;
            }
        }

        private async Task UpdatePlayerStatistics(GameSession gameSession, GameResult result)
        {
            if (gameSession.Player1 != null)
            {
                await UpdatePlayerStats(gameSession.Player1, result.Player1Won, gameSession);
            }
            else
            {
                _logger.LogWarning("Player1 object was null when trying to update stats for GameSession {GameId}", gameSession.GameId);
            }

            if (gameSession.Player2 != null)
            {
                await UpdatePlayerStats(gameSession.Player2, result.Player2Won, gameSession);
            }
            else
            {
                _logger.LogWarning("Player2 object was null when trying to update stats for GameSession {GameId}", gameSession.GameId);
            }
        }

        private async Task PersistGameSession(GameSession gameSession)
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);
            var entity = GameSessionEntity.FromModel(gameSession);
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            _logger.LogInformation("Saved/Updated GameSession {GameId} results to table storage.", gameSession.GameId);
        }

        // Helper method to update player statistics based on game results
        private async Task UpdatePlayerStats(Player player, bool isWinner, GameSession gameSession)
        {
            if (player == null)
            {
                _logger.LogWarning("Attempted to update stats for a null player in GameSession {GameId}", gameSession.GameId);
                return;
            }

            // Use domain service for scoring logic
            _scoringService.UpdatePlayerStats(player, isWinner, gameSession);

            // Save updated stats
            try
            {
                await _playerStorageService.UpdatePlayerAsync(player);
                _logger.LogInformation("Updated stats for player {PlayerInitials}", player.Initials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stats for player {PlayerInitials}", player.Initials);
                // Decide if this error should propagate
            }
        }
    }
}
