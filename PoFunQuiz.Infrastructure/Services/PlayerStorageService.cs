using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Storage;

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that manages player data persistence using Azure Table Storage
    /// </summary>
    public class PlayerStorageService : IPlayerStorageService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<PlayerStorageService> _logger;

        public PlayerStorageService(
            IOptions<TableStorageSettings> settings,
            ILogger<PlayerStorageService> logger)
        {
            var storageSettings = settings.Value;
            
            _tableClient = new TableClient(
                storageSettings.ConnectionString,
                storageSettings.TableName);
            
            // Ensure table exists
            _tableClient.CreateIfNotExists();
            
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Player> GetOrCreatePlayerAsync(string initials)
        {
            if (string.IsNullOrWhiteSpace(initials))
                throw new ArgumentException("Player initials cannot be empty", nameof(initials));
            
            try
            {
                // Try to get existing player
                var formattedInitials = initials.ToUpperInvariant();
                try
                {
                    var entity = await _tableClient.GetEntityAsync<PlayerEntity>("PLAYER", formattedInitials);
                    return ConvertToPlayerModel(entity.Value);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Player not found, create new
                    var newPlayer = new Player
                    {
                        Initials = formattedInitials,
                        GamesPlayed = 0,
                        GamesWon = 0,
                        TotalScore = 0,
                        TotalCorrectAnswers = 0,
                        LastPlayed = DateTime.UtcNow
                    };
                    
                    // Save to storage
                    await UpdatePlayerAsync(newPlayer);
                    
                    return newPlayer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating player with initials {Initials}", initials);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<Player>> GetAllPlayersAsync()
        {
            try
            {
                var players = new List<Player>();
                
                // Query all players from table storage
                var queryResults = _tableClient.QueryAsync<PlayerEntity>(filter: "");
                
                await foreach (var entity in queryResults)
                {
                    try
                    {
                        _logger.LogDebug("Processing player entity: PartitionKey={PartitionKey}, RowKey={RowKey}, LastPlayed={LastPlayed}",
                            entity.PartitionKey, entity.RowKey, entity.LastPlayed);
                        
                        players.Add(ConvertToPlayerModel(entity));
                    }
                    catch (FormatException ex)
                    {
                        // Log the error but continue processing other players
                        _logger.LogWarning(ex, "Error converting player entity with RowKey {RowKey}. Skipping this player.", entity.RowKey);
                    }
                    catch (Exception ex)
                    {
                        // Log any other errors but continue processing other players
                        _logger.LogWarning(ex, "Unexpected error processing player entity with RowKey {RowKey}. Skipping this player.", entity.RowKey);
                    }
                }
                
                _logger.LogInformation("Successfully loaded {Count} players from storage", players.Count);
                return players;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all players");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Player> UpdatePlayerAsync(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            
            try
            {
                // Validate Initials before proceeding
                if (string.IsNullOrWhiteSpace(player.Initials))
                {
                    throw new ArgumentException("Player Initials cannot be null or empty when updating.", nameof(player.Initials));
                }

                // Convert player model to entity
                var entity = new PlayerEntity
                {
                    PartitionKey = "PLAYER",
                    RowKey = player.Initials.ToUpperInvariant(), // Now safe due to check above
                    GamesPlayed = player.GamesPlayed,
                    GamesWon = player.GamesWon,
                    TotalScore = player.TotalScore,
                    TotalCorrectAnswers = player.TotalCorrectAnswers,
                    // Convert DateTime to DateTimeOffset?
                    // Assuming player.LastPlayed is UTC. If it might be local, adjust accordingly.
                    LastPlayed = new DateTimeOffset(player.LastPlayed, TimeSpan.Zero) 
                };
                
                // Save to table storage (upsert)
                await _tableClient.UpsertEntityAsync(entity);
                
                return player;
            }
            catch (Exception ex)
            {
                // Use null-coalescing for logging
                _logger.LogError(ex, "Error updating player {Initials}", player?.Initials ?? "[Unknown Initials]");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<Player>> GetTopPlayersAsync(int count)
        {
            try
            {
                var allPlayers = await GetAllPlayersAsync();
                
                // Sort by total score and take top N
                return allPlayers
                    .OrderByDescending(p => p.TotalScore)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top {Count} players", count);
                throw;
            }
        }
        
        // Helper method to convert PlayerEntity to Player model
        private Player ConvertToPlayerModel(PlayerEntity entity)
        {
            DateTime lastPlayed;

            // Directly use the DateTimeOffset? value from the entity
            if (entity.LastPlayed.HasValue)
            {
                // Convert the DateTimeOffset to the local DateTime for the Player model
                // Assuming the Player model expects local time. If it expects UTC, use .UtcDateTime
                lastPlayed = entity.LastPlayed.Value.LocalDateTime; 
                _logger.LogDebug("Converted LastPlayed from DateTimeOffset: {LastPlayed} (Local)", lastPlayed);
            }
            else
            {
                // Handle cases where LastPlayed might be null (e.g., corrupted data or older schema)
                _logger.LogWarning("LastPlayed value was null for player {RowKey}. Using UtcNow.", entity.RowKey ?? "[Unknown]");
                lastPlayed = DateTime.UtcNow; // Use UTC as a sensible default
            }

            return new Player
            {
                // Use null-coalescing for safety, although RowKey should be the initials
                Id = entity.RowKey ?? "[Unknown]", 
                Initials = entity.RowKey ?? "[Unknown]", // Assuming RowKey stores initials for PartitionKey "PLAYER"
                GamesPlayed = entity.GamesPlayed,
                GamesWon = entity.GamesWon,
                TotalScore = entity.TotalScore,
                TotalCorrectAnswers = entity.TotalCorrectAnswers,
                LastPlayed = lastPlayed // Assign the determined DateTime value
            };
        }
    }

    /// <summary>
    /// Configuration settings for Azure Table Storage
    /// </summary>
    public class TableStorageSettings
    {
        public required string ConnectionString { get; set; }
        public string TableName { get; set; } = "PlayerStats";
    }
}
