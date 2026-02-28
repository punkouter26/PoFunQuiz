using Microsoft.AspNetCore.SignalR;
using PoFunQuiz.Web.Features.Quiz;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class GameHub : Hub
{
    private readonly MultiplayerLobbyService _lobbyService;
    private readonly IServiceScopeFactory _scopeFactory;

    public GameHub(MultiplayerLobbyService lobbyService, IServiceScopeFactory scopeFactory)
    {
        _lobbyService = lobbyService;
        _scopeFactory = scopeFactory;
    }

    public async Task<string> CreateGame(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new HubException("Player name is required.");

        var session = _lobbyService.CreateSession(playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId);
        return session.GameId;
    }

    /// <summary>
    /// Returns a <see cref="JoinGameResult"/> — Success true on join, or a FailReason string
    /// ("not_found" | "already_started" | "already_full") for richer client-side messaging.
    /// </summary>
    public async Task<JoinGameResult> JoinGame(JoinGameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlayerName) || string.IsNullOrWhiteSpace(dto.GameId))
            return new JoinGameResult { Success = false, FailReason = "not_found" };

        if (_lobbyService.TryJoinSession(dto.GameId, dto.PlayerName, Context.ConnectionId, out var reason))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, dto.GameId);
            var session = _lobbyService.GetSession(dto.GameId);
            if (session != null)
            {
                await Clients.Group(dto.GameId).SendAsync("GameUpdated", _lobbyService.MapToDto(session));
                await Clients.Group(dto.GameId).SendAsync("PlayerJoined", dto.PlayerName);
            }
            return new JoinGameResult { Success = true };
        }

        var failReason = reason switch
        {
            JoinFailReason.AlreadyStarted => "already_started",
            JoinFailReason.AlreadyFull    => "already_full",
            _                             => "not_found"
        };
        return new JoinGameResult { Success = false, FailReason = failReason };
    }

    /// <summary>Returns all games currently waiting for a second player.</summary>
    public Task<IReadOnlyList<OpenGameInfo>> GetOpenGames()
        => Task.FromResult(_lobbyService.GetOpenGames());

    public async Task StartGame(string gameId, string topic)
    {
        // Authorization: only the host (Player 1) may start the game
        if (!_lobbyService.IsHost(gameId, Context.ConnectionId))
            throw new HubException("Only the host can start the game.");

        var session = _lobbyService.GetSession(gameId)
            ?? throw new HubException("Game not found.");

        // Generate questions via a DI scope (IOpenAIService is Scoped, hub is Transient)
        using var scope = _scopeFactory.CreateScope();
        var openAI = scope.ServiceProvider.GetRequiredService<IOpenAIService>();
        var questions = await openAI.GenerateQuizQuestionsAsync(topic, 5);

        // Both players receive the identical question set — answered independently
        session.Player1Questions = questions;
        session.Player2Questions = questions;
        session.StartTime = DateTime.UtcNow;

        await Clients.Group(gameId).SendAsync("GameStarted", _lobbyService.MapToDto(session));
    }

    /// <summary>
    /// Called when a remote player answers all questions or time expires.
    /// When both players report finished, broadcasts GameFinished to the group.
    /// </summary>
    public async Task PlayerFinished(string gameId, int playerNumber)
    {
        if (playerNumber is not (1 or 2)) return;

        var session = _lobbyService.GetSession(gameId);
        if (session is null) return;

        _lobbyService.MarkPlayerFinished(gameId, playerNumber);

        if (_lobbyService.AreBothPlayersFinished(gameId))
        {
            session.EndTime = DateTime.UtcNow;
            await Clients.Group(gameId).SendAsync("GameFinished", _lobbyService.MapToDto(session));
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
