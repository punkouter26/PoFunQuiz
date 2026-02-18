using System.Collections.Concurrent;
using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Multiplayer;

/// <summary>Reason a JoinSession attempt failed.</summary>
public enum JoinFailReason { None, NotFound, AlreadyStarted, AlreadyFull }

public class MultiplayerLobbyService
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _connectionToGame = new();
    // Maps gameId → host connectionId for authorization checks
    private readonly ConcurrentDictionary<string, string> _hostConnections = new();
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
                Player1 = new Player { Name = player1Name, Initials = player1Name[..Math.Min(3, player1Name.Length)].ToUpper() },
                Player2 = new Player { Name = "Waiting...", Initials = "?" },
                GameId = gameId
            };
        } while (!_sessions.TryAdd(gameId, session));

        _connectionToGame[connectionId] = gameId;
        _hostConnections[gameId] = connectionId;
        return session;
    }

    public GameSession? GetSession(string gameId)
    {
        _sessions.TryGetValue(gameId, out var session);
        return session;
    }

    /// <summary>Returns true if connectionId is the registered host of the game.</summary>
    public bool IsHost(string gameId, string connectionId)
    {
        return _hostConnections.TryGetValue(gameId, out var host) && host == connectionId;
    }

    public bool JoinSession(string gameId, string player2Name, string connectionId)
        => TryJoinSession(gameId, player2Name, connectionId, out _);

    public bool TryJoinSession(string gameId, string player2Name, string connectionId, out JoinFailReason reason)
    {
        reason = JoinFailReason.None;
        if (string.IsNullOrWhiteSpace(player2Name)) { reason = JoinFailReason.NotFound; return false; }

        if (!_sessions.TryGetValue(gameId, out var session))
        {
            reason = JoinFailReason.NotFound;
            return false;
        }

        lock (_joinLock)
        {
            if (session.StartTime.HasValue) { reason = JoinFailReason.AlreadyStarted; return false; }
            if (session.Player2.Name != "Waiting...") { reason = JoinFailReason.AlreadyFull; return false; }

            session.Player2 = new Player { Name = player2Name, Initials = player2Name[..Math.Min(3, player2Name.Length)].ToUpper() };
        }
        _connectionToGame[connectionId] = gameId;
        return true;
    }

    public void RemoveSession(string gameId)
    {
        _sessions.TryRemove(gameId, out _);
        _hostConnections.TryRemove(gameId, out _);
    }

    public void OnDisconnected(string connectionId)
    {
        if (_connectionToGame.TryRemove(connectionId, out var gameId))
        {
            // If the game hasn't started, clean it up
            if (_sessions.TryGetValue(gameId, out var session) && !session.StartTime.HasValue)
            {
                _sessions.TryRemove(gameId, out _);
                _hostConnections.TryRemove(gameId, out _);
            }
        }
    }

    /// <summary>Removes sessions that have been complete or idle for longer than <paramref name="maxAge"/>.</summary>
    public void PurgeExpiredSessions(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        foreach (var (gameId, session) in _sessions)
        {
            bool expired = session.EndTime.HasValue
                ? session.EndTime.Value < cutoff
                : session.StartTime.HasValue
                    ? session.StartTime.Value < cutoff
                    : false; // pre-start sessions are cleaned by OnDisconnected

            if (expired) RemoveSession(gameId);
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
