using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Web.Services
{
    /// <summary>
    /// Service for managing game state between pages
    /// </summary>
    public class GameState
    {
        public GameSession? CurrentGame { get; set; }
    }
}