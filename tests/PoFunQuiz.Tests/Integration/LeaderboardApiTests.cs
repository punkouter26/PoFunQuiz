using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoFunQuiz.Web.Features.Leaderboard;
using PoFunQuiz.Web.Models;
using Xunit;

namespace PoFunQuiz.Tests.Integration
{
    /// <summary>
    /// Uses TestWebApplicationFactory (with MockOpenAIService) so this class shares the
    /// same in-proc test host isolation as all other integration suites — no raw
    /// WebApplicationFactory&lt;Program&gt; which would spin up a second host with live Azure deps.
    /// </summary>
    public class LeaderboardApiTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _baseFactory;
        private readonly Mock<ILeaderboardRepository> _mockRepo;
        private readonly HttpClient _client;

        public LeaderboardApiTests(TestWebApplicationFactory factory)
        {
            _mockRepo = new Mock<ILeaderboardRepository>();
            _baseFactory = factory;
            // Layer an additional WithWebHostBuilder on top of the shared factory so the
            // mock repo is scoped to this test class only, without polluting others.
            var scopedFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockRepo.Object);
                });
            });
            _client = scopedFactory.CreateClient();
        }

        [Fact]
        public async Task GetLeaderboard_ReturnsOkAndList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTopScoresAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<LeaderboardEntry>
                {
                    new LeaderboardEntry { PlayerName = "Test", Score = 100 }
                });

            // Act
            var response = await _client.GetAsync("/api/leaderboard");

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
            var entry = new LeaderboardEntry { PlayerName = "Test", Score = 100 };
            _mockRepo.Setup(r => r.AddScoreAsync(It.IsAny<LeaderboardEntry>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await _client.PostAsJsonAsync("/api/leaderboard", entry);

            // Assert
            response.EnsureSuccessStatusCode();
            _mockRepo.Verify(r => r.AddScoreAsync(It.IsAny<LeaderboardEntry>()), Times.Once);
        }
    }
}
