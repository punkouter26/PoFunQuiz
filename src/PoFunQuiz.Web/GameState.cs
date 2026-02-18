using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web
{
    /// <summary>
    /// Scoped service for managing game state between pages.
    /// Encapsulates mutation via SetGame/ClearGame and notifies subscribers via OnChange.
    /// </summary>
    public class GameState
    {
        private GameSession? _currentGame;

        /// <summary>The active game session. Null when no game is in progress.</summary>
        public GameSession? CurrentGame => _currentGame;

        /// <summary>Raised whenever the game state changes — subscribe in Blazor components to trigger re-render.</summary>
        public event Action? OnChange;

        /// <summary>Sets a new active game session and notifies subscribers.</summary>
        public void SetGame(GameSession session)
        {
            _currentGame = session;
            NotifyStateChanged();
        }

        /// <summary>Clears the active game session and notifies subscribers.</summary>
        public void ClearGame()
        {
            _currentGame = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
