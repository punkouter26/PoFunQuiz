using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Infrastructure.Services;
using Xunit;

namespace PoFunQuiz.Tests.Unit
{
    public class LeaderboardRepositoryTests
    {
        private readonly Mock<TableClient> _mockTableClient;
        private readonly Mock<ILogger<LeaderboardRepository>> _mockLogger;
        private readonly LeaderboardRepository _repository;

        public LeaderboardRepositoryTests()
        {
            _mockTableClient = new Mock<TableClient>();
            _mockLogger = new Mock<ILogger<LeaderboardRepository>>();

            // Mock CreateIfNotExists
            _mockTableClient.Setup(x => x.CreateIfNotExists(default)).Returns((Response<Azure.Data.Tables.Models.TableItem>)null!);

            _repository = new LeaderboardRepository(_mockTableClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddScoreAsync_ShouldCallAddEntityAsync()
        {
            // Arrange
            var entry = new LeaderboardEntry { PartitionKey = "General", RowKey = "User1", Score = 100 };

            // Act
            await _repository.AddScoreAsync(entry);

            // Assert
            _mockTableClient.Verify(x => x.AddEntityAsync(entry, default), Times.Once);
        }

        [Fact]
        public async Task GetTopScoresAsync_ShouldReturnSortedScores()
        {
            // Arrange
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { PartitionKey = "General", RowKey = "User1", Score = 50 },
                new LeaderboardEntry { PartitionKey = "General", RowKey = "User2", Score = 100 },
                new LeaderboardEntry { PartitionKey = "General", RowKey = "User3", Score = 75 }
            };

            var page = Page<LeaderboardEntry>.FromValues(entries, null, Mock.Of<Response>());
            var pages = AsyncPageable<LeaderboardEntry>.FromPages(new[] { page });

            _mockTableClient.Setup(x => x.QueryAsync<LeaderboardEntry>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(pages);

            // Act
            var result = await _repository.GetTopScoresAsync("General", 10);

            // Assert
            result.Should().HaveCount(3);
            result[0].Score.Should().Be(100);
            result[1].Score.Should().Be(75);
            result[2].Score.Should().Be(50);
        }
    }
}
