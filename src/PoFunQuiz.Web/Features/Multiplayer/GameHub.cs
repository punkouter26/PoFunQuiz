using Microsoft.AspNetCore.SignalR;

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
        if (string.IsNullOrWhiteSpace(playerName))
            throw new HubException("Player name is required.");

        var session = _lobbyService.CreateSession(playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId);
        return session.GameId;
    }

    public async Task<bool> JoinGame(JoinGameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlayerName) || string.IsNullOrWhiteSpace(dto.GameId))
            return false;

        if (_lobbyService.JoinSession(dto.GameId, dto.PlayerName, Context.ConnectionId))
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
        if (playerNumber is not (1 or 2)) return;

        var session = _lobbyService.GetSession(gameId);
        if (session != null)
        {
            if (playerNumber == 1) session.Player1BaseScore = score;
            else session.Player2BaseScore = score;

            await Clients.Group(gameId).SendAsync("ScoreUpdated", _lobbyService.MapToDto(session));
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _lobbyService.OnDisconnected(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
