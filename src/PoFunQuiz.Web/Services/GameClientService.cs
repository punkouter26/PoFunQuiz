using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PoFunQuiz.Shared.Contracts;
using System;
using System.Threading.Tasks;

namespace PoFunQuiz.Web.Services
{
    public class GameClientService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly NavigationManager _navigationManager;

        public event Action<GameStateDto>? OnGameUpdated;
        public event Action<string>? OnPlayerJoined;
        public event Action<GameStateDto>? OnGameStarted;
        public event Action<GameStateDto>? OnScoreUpdated;

        public GameClientService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public async Task InitializeAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/gamehub"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<GameStateDto>("GameUpdated", (state) => OnGameUpdated?.Invoke(state));
            _hubConnection.On<string>("PlayerJoined", (name) => OnPlayerJoined?.Invoke(name));
            _hubConnection.On<GameStateDto>("GameStarted", (state) => OnGameStarted?.Invoke(state));
            _hubConnection.On<GameStateDto>("ScoreUpdated", (state) => OnScoreUpdated?.Invoke(state));

            await _hubConnection.StartAsync();
        }

        public async Task<string> CreateGameAsync(string playerName)
        {
            if (_hubConnection is null) return string.Empty;
            return await _hubConnection.InvokeAsync<string>("CreateGame", playerName);
        }

        public async Task<bool> JoinGameAsync(string gameId, string playerName)
        {
            if (_hubConnection is null) return false;
            return await _hubConnection.InvokeAsync<bool>("JoinGame", new JoinGameDto { GameId = gameId, PlayerName = playerName });
        }

        public async Task StartGameAsync(string gameId)
        {
            if (_hubConnection is null) return;
            await _hubConnection.InvokeAsync("StartGame", gameId);
        }

        public async Task UpdateScoreAsync(string gameId, int playerNumber, int score)
        {
            if (_hubConnection is null) return;
            await _hubConnection.InvokeAsync("UpdateScore", gameId, playerNumber, score);
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
