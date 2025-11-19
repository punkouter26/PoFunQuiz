using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using FluentAssertions;

namespace PoFunQuiz.Tests.Integration
{
    public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GameHubTests(WebApplicationFactory<Program> factory)
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
