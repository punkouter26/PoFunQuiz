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
            // Arrange
            var hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost/gamehub", o =>
                {
                    o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
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
