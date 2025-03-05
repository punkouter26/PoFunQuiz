using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that manages game sessions and results
    /// </summary>
    public class GameSessionService : IGameSessionService
    {
        private readonly IPlayerStorageService _playerStorageService;
        private readonly ILogger<GameSessionService> _logger;
        
        // In a real application, we would store game sessions in persistent storage
        // For this implementation, we'll use an in-memory collection for simplicity
        private static readonly List<GameSession> _gameSessions = new List<GameSession>();

        public GameSessionService(
            IPlayerStorageService playerStorageService,
            ILogger<GameSessionService> logger)
        {
            _playerStorageService = playerStorageService;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<GameSession> CreateGameSessionAsync(Player player1, Player player2)
        {
            if (player1 == null)
                throw new ArgumentNullException(nameof(player1));
            
            if (player2 == null)
                throw new ArgumentNullException(nameof(player2));
            
            try
            {
                // Create new game session
                var gameSession = new GameSession
                {
                    GameId = Guid.NewGuid().ToString(),
                    Player1 = player1,
                    Player2 = player2,
                    StartTime = DateTime.UtcNow
                };
                
                // Add to in-memory collection
                _gameSessions.Add(gameSession);
                
                return Task.FromResult(gameSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game session for players {Player1} and {Player2}", 
                    player1.Initials, player2.Initials);
                throw;
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
                await UpdatePlayerStats(gameSession.Player1, player1Won, gameSession);
                await UpdatePlayerStats(gameSession.Player2, !isTie && !player1Won, gameSession);
                
                // In a real application, we would save the game session to a database
                // For this implementation, we simply update the in-memory collection
                int existingIndex = _gameSessions.FindIndex(g => g.GameId == gameSession.GameId);
                if (existingIndex >= 0)
                {
                    _gameSessions[existingIndex] = gameSession;
                }
                else
                {
                    _gameSessions.Add(gameSession);
                }
                
                return gameSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving game session results for game {GameId}", gameSession.GameId);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<List<GameSession>> GetRecentGameSessionsAsync(string? playerInitials = default, int count = 10)
        {
            try
            {
                // Filter and sort game sessions
                var filteredSessions = new List<GameSession>(_gameSessions);
                
                // Filter by player, if specified
                if (!string.IsNullOrWhiteSpace(playerInitials))
                {
                    string formattedInitials = playerInitials.ToUpperInvariant();
                    filteredSessions = filteredSessions.FindAll(g => 
                        g.Player1.Initials == formattedInitials || 
                        g.Player2.Initials == formattedInitials);
                }
                
                // Sort by date (most recent first) and take the specified count
                filteredSessions.Sort((a, b) => 
                    b.EndTime.HasValue && a.EndTime.HasValue ? 
                    b.EndTime.Value.CompareTo(a.EndTime.Value) : 
                    0);
                
                if (filteredSessions.Count > count)
                {
                    filteredSessions = filteredSessions.GetRange(0, count);
                }
                
                return Task.FromResult(filteredSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent game sessions");
                throw;
            }
        }
        
        // Helper method to update player statistics based on game results
        private async Task UpdatePlayerStats(Player player, bool isWinner, GameSession gameSession)
        {
            // Calculate player stats from game session
            int playerScore = player == gameSession.Player1 ? gameSession.Player1Score : gameSession.Player2Score;
            int correctAnswers = Math.Max(0, playerScore - 3); // Assuming max 3 points time bonus
            
            // Update player stats
            player.GamesPlayed++;
            if (isWinner) player.GamesWon++;
            player.TotalScore += playerScore;
            player.TotalCorrectAnswers += correctAnswers;
            player.LastPlayed = DateTime.UtcNow;
            
            // Save updated stats
            await _playerStorageService.UpdatePlayerAsync(player);
        }
    }
}