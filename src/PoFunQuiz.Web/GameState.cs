using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web
{
    /// <summary>
    /// Service for managing game state between pages.
    /// </summary>
    public class GameState
    {
        public GameSession? CurrentGame { get; set; }
    }
}
