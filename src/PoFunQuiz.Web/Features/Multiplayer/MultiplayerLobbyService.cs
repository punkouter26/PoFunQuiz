using System.Collections.Concurrent;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Shared.Contracts;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class MultiplayerLobbyService
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession CreateSession(string player1Name)
    {
        var session = new GameSession
        {
            Player1 = new Player { Name = player1Name },
            Player2 = new Player { Name = "Waiting..." },
            Player1Initials = player1Name.Substring(0, Math.Min(3, player1Name.Length)).ToUpper(),
            GameId = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
        };
        _sessions.TryAdd(session.GameId, session);
        return session;
    }

    public GameSession? GetSession(string gameId)
    {
        _sessions.TryGetValue(gameId, out var session);
        return session;
    }

    public bool JoinSession(string gameId, string player2Name)
    {
        if (_sessions.TryGetValue(gameId, out var session))
        {
            if (session.Player2.Name != "Waiting...") return false;

            session.Player2 = new Player { Name = player2Name };
            session.Player2Initials = player2Name.Substring(0, Math.Min(3, player2Name.Length)).ToUpper();
            return true;
        }
        return false;
    }

    public void RemoveSession(string gameId)
    {
        _sessions.TryRemove(gameId, out _);
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
