using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoFunQuiz.Core.Interfaces;
using PoFunQuiz.Core.Models;
using Xunit;

namespace PoFunQuiz.Tests.Integration
{
    public class LeaderboardApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<ILeaderboardRepository> _mockRepo;

        public LeaderboardApiTests(WebApplicationFactory<Program> factory)
        {
            _mockRepo = new Mock<ILeaderboardRepository>();
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockRepo.Object);
                });
            });
        }

        [Fact]
        public async Task GetLeaderboard_ReturnsOkAndList()
        {
            // Arrange
            var client = _factory.CreateClient();
            _mockRepo.Setup(r => r.GetTopScoresAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<LeaderboardEntry>
                {
                    new LeaderboardEntry { PlayerName = "Test", Score = 100 }
                });

            // Act
            var response = await client.GetAsync("/api/leaderboard");

            // Assert
            response.EnsureSuccessStatusCode();
            var entries = await response.Content.ReadFromJsonAsync<List<LeaderboardEntry>>();
            Assert.NotNull(entries);
            Assert.Single(entries);
            Assert.Equal("Test", entries[0].PlayerName);
        }

        [Fact]
        public async Task SubmitScore_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var entry = new LeaderboardEntry { PlayerName = "Test", Score = 100 };
            _mockRepo.Setup(r => r.AddScoreAsync(It.IsAny<LeaderboardEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await client.PostAsJsonAsync("/api/leaderboard", entry);

            // Assert
            response.EnsureSuccessStatusCode();
            _mockRepo.Verify(r => r.AddScoreAsync(It.IsAny<LeaderboardEntry>()), Times.Once);
        }
    }
}
