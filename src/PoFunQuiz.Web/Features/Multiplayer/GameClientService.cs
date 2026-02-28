using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class GameClientService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;

    public event Action<GameStateDto>? OnGameUpdated;
    public event Action<string>? OnPlayerJoined;
    public event Action<GameStateDto>? OnGameStarted;
    public event Action<GameStateDto>? OnScoreUpdated;
    /// <summary>Raised when the server signals both remote players have finished.</summary>
    public event Action<GameStateDto>? OnGameFinished;

    public GameClientService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public async Task InitializeAsync()
    {
        if (_hubConnection?.State is HubConnectionState.Connected or HubConnectionState.Connecting)
            return;

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<GameStateDto>("GameUpdated", (state) => OnGameUpdated?.Invoke(state));
        _hubConnection.On<string>("PlayerJoined", (name) => OnPlayerJoined?.Invoke(name));
        _hubConnection.On<GameStateDto>("GameStarted", (state) => OnGameStarted?.Invoke(state));
        _hubConnection.On<GameStateDto>("ScoreUpdated", (state) => OnScoreUpdated?.Invoke(state));
        _hubConnection.On<GameStateDto>("GameFinished", (state) => OnGameFinished?.Invoke(state));

        await _hubConnection.StartAsync();
    }

    public async Task<string> CreateGameAsync(string playerName)
    {
        if (_hubConnection is null) return string.Empty;
        return await _hubConnection.InvokeAsync<string>("CreateGame", playerName);
    }

    public async Task<List<OpenGameInfo>> GetOpenGamesAsync()
    {
        if (_hubConnection is null) return [];
        var result = await _hubConnection.InvokeAsync<IReadOnlyList<OpenGameInfo>>("GetOpenGames");
        return result.ToList();
    }

    public async Task<JoinGameResult> JoinGameAsync(string gameId, string playerName)
    {
        if (_hubConnection is null) return new JoinGameResult { Success = false, FailReason = "not_found" };
        return await _hubConnection.InvokeAsync<JoinGameResult>("JoinGame", new JoinGameDto { GameId = gameId, PlayerName = playerName });
    }

    public async Task StartGameAsync(string gameId, string topic)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("StartGame", gameId, topic);
    }

    public async Task UpdateScoreAsync(string gameId, int playerNumber, int score)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("UpdateScore", gameId, playerNumber, score);
    }

    /// <summary>Notifies the server that this player has finished answering all questions.</summary>
    public async Task PlayerFinishedAsync(string gameId, int playerNumber)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("PlayerFinished", gameId, playerNumber);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
