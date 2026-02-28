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

        // ── Remote multiplayer context ──────────────────────────────────────
        /// <summary>True when the current game was started via remote multiplayer (SignalR).</summary>
        public bool IsRemoteMode { get; private set; }

        /// <summary>Which seat this device occupies in a remote game (1 or 2).</summary>
        public int LocalPlayerNumber { get; private set; } = 1;

        /// <summary>The SignalR game-room ID used to push score updates to the opponent.</summary>
        public string? RemoteGameId { get; private set; }

        /// <summary>Raised whenever the game state changes — subscribe in Blazor components to trigger re-render.</summary>
        public event Action? OnChange;

        /// <summary>Sets a new active game session and notifies subscribers.</summary>
        public void SetGame(GameSession session)
        {
            _currentGame = session;
            NotifyStateChanged();
        }

        /// <summary>Attaches remote multiplayer context so GameBoard knows to render single-player mode.</summary>
        public void SetRemoteContext(int playerNumber, string gameId)
        {
            IsRemoteMode = true;
            LocalPlayerNumber = playerNumber;
            RemoteGameId = gameId;
        }

        /// <summary>Clears the active game session and resets remote context.</summary>
        public void ClearGame()
        {
            _currentGame = null;
            IsRemoteMode = false;
            LocalPlayerNumber = 1;
            RemoteGameId = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
