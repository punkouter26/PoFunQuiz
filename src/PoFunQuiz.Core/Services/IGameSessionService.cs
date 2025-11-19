using System.Collections.Generic;
using System.Threading.Tasks;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Services
{
    /// <summary>
    /// Service interface for managing game sessions
    /// </summary>
    public interface IGameSessionService
    {
        /// <summary>
        /// Creates a new game session with the specified players
        /// </summary>
        /// <param name="player1">Player 1</param>
        /// <param name="player2">Player 2</param>
        /// <returns>The created game session</returns>
        Task<GameSession> CreateGameSessionAsync(Player player1, Player player2);

        /// <summary>
        /// Saves the results of a completed game session
        /// </summary>
        /// <param name="gameSession">The completed game session with results</param>
        /// <returns>The updated game session</returns>
        Task<GameSession> SaveGameResultsAsync(GameSession gameSession);

        /// <summary>
        /// Gets recent game sessions, optionally filtered by player initials
        /// </summary>
        /// <param name="playerInitials">Optional player initials to filter by</param>
        /// <param name="count">Number of sessions to retrieve</param>
        /// <returns>List of recent game sessions</returns>
        Task<List<GameSession>> GetRecentGameSessionsAsync(string? playerInitials = null, int count = 10); // Made playerInitials nullable
    }
}
