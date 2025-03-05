using System.Collections.Generic;
using System.Threading.Tasks;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Services
{
    /// <summary>
    /// Service interface for managing player data storage
    /// </summary>
    public interface IPlayerStorageService
    {
        /// <summary>
        /// Gets a player by their initials, or creates a new player if not found
        /// </summary>
        /// <param name="initials">Player's 3-letter initials</param>
        /// <returns>The player object</returns>
        Task<Player> GetOrCreatePlayerAsync(string initials);
        
        /// <summary>
        /// Gets all players from storage
        /// </summary>
        /// <returns>List of all players</returns>
        Task<List<Player>> GetAllPlayersAsync();
        
        /// <summary>
        /// Updates a player's information in storage
        /// </summary>
        /// <param name="player">Player object with updated information</param>
        /// <returns>The updated player object</returns>
        Task<Player> UpdatePlayerAsync(Player player);
        
        /// <summary>
        /// Gets the top N players based on total score
        /// </summary>
        /// <param name="count">Number of top players to retrieve</param>
        /// <returns>List of top players</returns>
        Task<List<Player>> GetTopPlayersAsync(int count);
    }
}