using System.Collections.Concurrent;
using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class MultiplayerLobbyService
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _connectionToGame = new();
    private readonly object _joinLock = new();

    public GameSession CreateSession(string player1Name, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(player1Name))
            throw new ArgumentException("Player name is required.", nameof(player1Name));

        string gameId;
        GameSession session;
        do
        {
            gameId = Guid.NewGuid().ToString()[..6].ToUpper();
            session = new GameSession
            {
                Player1 = new Player { Name = player1Name },
                Player2 = new Player { Name = "Waiting..." },
                Player1Initials = player1Name[..Math.Min(3, player1Name.Length)].ToUpper(),
                GameId = gameId
            };
        } while (!_sessions.TryAdd(gameId, session));

        _connectionToGame[connectionId] = gameId;
        return session;
    }

    public GameSession? GetSession(string gameId)
    {
        _sessions.TryGetValue(gameId, out var session);
        return session;
    }

    public bool JoinSession(string gameId, string player2Name, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(player2Name)) return false;

        if (_sessions.TryGetValue(gameId, out var session))
        {
            lock (_joinLock)
            {
                if (session.Player2.Name != "Waiting...") return false;

                session.Player2 = new Player { Name = player2Name };
                session.Player2Initials = player2Name[..Math.Min(3, player2Name.Length)].ToUpper();
            }
            _connectionToGame[connectionId] = gameId;
            return true;
        }
        return false;
    }

    public void RemoveSession(string gameId)
    {
        _sessions.TryRemove(gameId, out _);
    }

    public void OnDisconnected(string connectionId)
    {
        if (_connectionToGame.TryRemove(connectionId, out var gameId))
        {
            // If the game hasn't started, clean it up
            if (_sessions.TryGetValue(gameId, out var session) && !session.StartTime.HasValue)
            {
                _sessions.TryRemove(gameId, out _);
            }
        }
    }

    public GameStateDto MapToDto(GameSession session)
    {
        return new GameStateDto
        {
            GameId = session.GameId,
            Player1Name = session.Player1.Name,
            Player2Name = session.Player2.Name,
            Player1Score = session.Player1Score,
            Player2Score = session.Player2Score,
            IsGameStarted = session.StartTime.HasValue,
            IsGameOver = session.IsComplete
        };
    }
}
