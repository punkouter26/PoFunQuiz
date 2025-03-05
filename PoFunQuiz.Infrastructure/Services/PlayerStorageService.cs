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
                // Convert player model to entity
                var entity = new PlayerEntity
                {
                    PartitionKey = "PLAYER",
                    RowKey = player.Initials.ToUpperInvariant(),
                    GamesPlayed = player.GamesPlayed,
                    GamesWon = player.GamesWon,
                    TotalScore = player.TotalScore,
                    TotalCorrectAnswers = player.TotalCorrectAnswers,
                    LastPlayed = player.LastPlayed
                };
                
                // Save to table storage (upsert)
                await _tableClient.UpsertEntityAsync(entity);
                
                return player;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating player {Initials}", player.Initials);
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
            
            try
            {
                // Get the LastPlayed value
                var lastPlayedValue = entity.LastPlayed;
                
                _logger.LogDebug("Processing LastPlayed value: {LastPlayed}, Type: {Type}", 
                    lastPlayedValue, lastPlayedValue != null ? lastPlayedValue.GetType().FullName : "null");

                if (lastPlayedValue is DateTime dateTime)
                {
                    lastPlayed = dateTime;
                    _logger.LogDebug("LastPlayed is already a DateTime: {LastPlayed}", lastPlayed);
                }
                else if (lastPlayedValue is DateTimeOffset)
                {
                    lastPlayed = ((DateTimeOffset)lastPlayedValue).DateTime;
                    _logger.LogDebug("LastPlayed is a DateTimeOffset: {LastPlayed}", lastPlayed);
                }
                else
                {
                    // Convert to string and try parsing
                    var lastPlayedString = lastPlayedValue != null ? lastPlayedValue.ToString() : string.Empty;
                    _logger.LogDebug("LastPlayed string value: {LastPlayedString}", lastPlayedString);

                    // Handle the @ prefix that Azure Table Storage sometimes adds
                    if (lastPlayedString.StartsWith('@'))
                    {
                        lastPlayedString = lastPlayedString.Substring(1);
                        _logger.LogDebug("Removed @ prefix, new value: {LastPlayedString}", lastPlayedString);
                    }
                    
                    // Try parsing with multiple formats and styles
                    if (DateTime.TryParse(lastPlayedString, out DateTime parsedDate))
                    {
                        lastPlayed = parsedDate;
                        _logger.LogDebug("Successfully parsed LastPlayed with TryParse: {ParsedDate}", lastPlayed);
                    }
                    else if (DateTimeOffset.TryParse(lastPlayedString, out DateTimeOffset parsedDateOffset))
                    {
                        lastPlayed = parsedDateOffset.DateTime;
                        _logger.LogDebug("Successfully parsed LastPlayed as DateTimeOffset: {ParsedDate}", lastPlayed);
                    }
                    else
                    {
                        // Try with specific formats
                        string[] formats = new string[] 
                        { 
                            "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                            "yyyy-MM-ddTHH:mm:ss.fffffff",
                            "yyyy-MM-ddTHH:mm:ss",
                            "yyyy-MM-dd"
                        };
                        
                        if (DateTime.TryParseExact(lastPlayedString, formats, 
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AdjustToUniversal | 
                            System.Globalization.DateTimeStyles.AssumeLocal, 
                            out DateTime parsedExactDate))
                        {
                            lastPlayed = parsedExactDate;
                            _logger.LogDebug("Successfully parsed LastPlayed with TryParseExact: {ParsedDate}", lastPlayed);
                        }
                        else
                        {
                            // Last resort: try to manually parse the ISO 8601 format
                            try
                            {
                                // For format like: 2025-03-03T18:06:46.0179030-05:00
                                var parts = lastPlayedString.Split('T');
                                if (parts.Length == 2)
                                {
                                    var datePart = parts[0];
                                    var timePart = parts[1];
                                    
                                    // Split time and timezone
                                    var timezoneSplit = timePart.LastIndexOfAny(new char[] { '+', '-' });
                                    if (timezoneSplit > 0)
                                    {
                                        var timeWithoutTz = timePart.Substring(0, timezoneSplit);
                                        var timezone = timePart.Substring(timezoneSplit);
                                        
                                        // Reconstruct in a format that .NET can parse
                                        var reconstructed = $"{datePart}T{timeWithoutTz}{timezone}";
                                        if (DateTimeOffset.TryParse(reconstructed, out DateTimeOffset dto))
                                        {
                                            lastPlayed = dto.DateTime;
                                            _logger.LogDebug("Successfully parsed LastPlayed with manual reconstruction: {ParsedDate}", lastPlayed);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Failed to parse LastPlayed value after reconstruction: {LastPlayed}", reconstructed);
                                            lastPlayed = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to find timezone in LastPlayed value: {LastPlayed}", lastPlayedString);
                                        lastPlayed = DateTime.UtcNow;
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to split LastPlayed into date and time parts: {LastPlayed}", lastPlayedString);
                                    lastPlayed = DateTime.UtcNow;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error during manual parsing of LastPlayed: {LastPlayed}", lastPlayedString);
                                lastPlayed = DateTime.UtcNow;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling LastPlayed value: {LastPlayed}", entity.LastPlayed);
                lastPlayed = DateTime.UtcNow;
            }

            return new Player
            {
                Id = entity.RowKey,
                Initials = entity.PartitionKey == "PLAYER" ? entity.RowKey : entity.PartitionKey,
                GamesPlayed = entity.GamesPlayed,
                GamesWon = entity.GamesWon,
                TotalScore = entity.TotalScore,
                TotalCorrectAnswers = entity.TotalCorrectAnswers,
                LastPlayed = lastPlayed
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