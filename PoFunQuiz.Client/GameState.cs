using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Client
{
    /// <summary>
    /// Service for managing game state between pages
    /// </summary>
    public class GameState
    {
        public PoFunQuiz.Core.Models.GameSession? CurrentGame { get; set; }
    }
}
