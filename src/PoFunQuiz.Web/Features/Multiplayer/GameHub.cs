using Microsoft.AspNetCore.SignalR;
using PoFunQuiz.Shared.Contracts;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class GameHub : Hub
{
    private readonly MultiplayerLobbyService _lobbyService;

    public GameHub(MultiplayerLobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    public async Task<string> CreateGame(string playerName)
    {
        var session = _lobbyService.CreateSession(playerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId);
        return session.GameId;
    }

    public async Task<bool> JoinGame(JoinGameDto dto)
    {
        if (_lobbyService.JoinSession(dto.GameId, dto.PlayerName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, dto.GameId);
            var session = _lobbyService.GetSession(dto.GameId);
            if (session != null)
            {
                await Clients.Group(dto.GameId).SendAsync("GameUpdated", _lobbyService.MapToDto(session));
                await Clients.Group(dto.GameId).SendAsync("PlayerJoined", dto.PlayerName);
            }
            return true;
        }
        return false;
    }

    public async Task StartGame(string gameId)
    {
        var session = _lobbyService.GetSession(gameId);
        if (session != null)
        {
            session.StartTime = DateTime.UtcNow;
            await Clients.Group(gameId).SendAsync("GameStarted", _lobbyService.MapToDto(session));
        }
    }

    public async Task UpdateScore(string gameId, int playerNumber, int score)
    {
        var session = _lobbyService.GetSession(gameId);
        if (session != null)
        {
            if (playerNumber == 1) session.Player1BaseScore = score;
            else session.Player2BaseScore = score;

            await Clients.Group(gameId).SendAsync("ScoreUpdated", _lobbyService.MapToDto(session));
        }
    }
}
