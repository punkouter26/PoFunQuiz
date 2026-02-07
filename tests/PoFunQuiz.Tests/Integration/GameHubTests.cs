using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using FluentAssertions;

namespace PoFunQuiz.Tests.Integration
{
    /// <summary>
    /// Integration tests for SignalR GameHub using mocked services
    /// </summary>
    public class GameHubTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public GameHubTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateGame_ShouldReturnGameId()
        {
            // Arrange — use the test server's handler to avoid port/redirect issues
            var server = _factory.Server;
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(server.BaseAddress, "/gamehub"), o =>
                {
                    o.HttpMessageHandlerFactory = _ => server.CreateHandler();
                })
                .Build();

            await hubConnection.StartAsync();

            // Act
            var gameId = await hubConnection.InvokeAsync<string>("CreateGame", "Player1");

            // Assert
            gameId.Should().NotBeNullOrEmpty();

            await hubConnection.StopAsync();
        }
    }
}
