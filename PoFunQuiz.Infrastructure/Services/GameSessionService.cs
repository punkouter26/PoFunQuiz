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
        private readonly ILogger<GameSessionService> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private const string TableName = "GameSessions";

        public GameSessionService(
            IPlayerStorageService playerStorageService,
            ILogger<GameSessionService> logger,
            TableServiceClient tableServiceClient) // Inject TableServiceClient
        {
            _playerStorageService = playerStorageService;
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            // Ensure table exists (consider moving to startup if preferred)
            try
            {
                _tableServiceClient.CreateTableIfNotExists(TableName);
                _logger.LogInformation("Ensured Azure Table '{TableName}' exists.", TableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure Azure Table '{TableName}' exists. Check connection string and permissions.", TableName);
                // Depending on requirements, might want to throw or handle differently
            }
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
            if (gameSession == null)
                throw new ArgumentNullException(nameof(gameSession));

            try
            {
                // Ensure the game is marked as complete
                if (!gameSession.EndTime.HasValue)
                {
                    gameSession.EndTime = DateTime.UtcNow;
                }

                // Determine winner
                bool isTie = gameSession.Player1Score == gameSession.Player2Score;
                bool player1Won = gameSession.Player1Score > gameSession.Player2Score;

                // Update player statistics
                // Ensure Player objects are not null before updating stats
                // Note: The GameSession passed might not have full Player objects if retrieved from storage.
                // Fetch them if necessary, or ensure the calling code provides them.
                // For now, assume Player1/Player2 objects are present if needed for UpdatePlayerStats.
                if (gameSession.Player1 != null)
                    await UpdatePlayerStats(gameSession.Player1, player1Won, gameSession);
                else
                    _logger.LogWarning("Player1 object was null when trying to update stats for GameSession {GameId}", gameSession.GameId);

                if (gameSession.Player2 != null)
                    await UpdatePlayerStats(gameSession.Player2, !isTie && !player1Won, gameSession);
                else
                    _logger.LogWarning("Player2 object was null when trying to update stats for GameSession {GameId}", gameSession.GameId);

                // Save/Update the game session entity in Table Storage
                var tableClient = _tableServiceClient.GetTableClient(TableName);
                var entity = GameSessionEntity.FromModel(gameSession);
                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace); // Use Upsert with Replace mode
                _logger.LogInformation("Saved/Updated GameSession {GameId} results to table storage.", gameSession.GameId);

                return gameSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving game session results for game {GameId} to table storage", gameSession.GameId);
                throw; // Rethrow to indicate failure
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

        // Helper method to update player statistics based on game results
        private async Task UpdatePlayerStats(Player player, bool isWinner, GameSession gameSession)
        {
            if (player == null)
            {
                _logger.LogWarning("Attempted to update stats for a null player in GameSession {GameId}", gameSession.GameId);
                return;
            }

            // Calculate player stats from game session
            int playerScore = player.Initials == gameSession.Player1Initials ? gameSession.Player1Score : gameSession.Player2Score;
            // Correct answer calculation might need refinement based on actual scoring logic
            int correctAnswers = Math.Max(0, playerScore); // Simplistic assumption: score = correct answers
            if (playerScore > 10) // If score includes bonus, try to deduce correct answers
            {
                // This is heuristic, assumes bonus calculation logic elsewhere might be complex
                correctAnswers = Math.Min(10, playerScore - (playerScore > 10 ? (playerScore - 10) : 0));
                _logger.LogWarning("Calculated correct answers heuristically as {CorrectCount} based on score {Score} for player {PlayerInitials}", correctAnswers, playerScore, player.Initials);
            }

            // Update player stats
            player.GamesPlayed++;
            if (isWinner) player.GamesWon++;
            player.TotalScore += playerScore;
            player.TotalCorrectAnswers += correctAnswers; // Add calculated correct answers
            player.LastPlayed = DateTime.UtcNow;

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
