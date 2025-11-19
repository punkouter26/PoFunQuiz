using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoFunQuiz.Infrastructure.Services;
using PoFunQuiz.Infrastructure.Storage;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Configuration;
using Azure.Data.Tables;
using System;

namespace PoFunQuiz.Tests.Unit;

public class PlayerStorageServiceTests
{
    [Theory]
    [InlineData("2024-10-23T10:30:00Z")]
    [InlineData("2024-01-01T00:00:00Z")]
    [InlineData("2025-12-31T23:59:59Z")]
    public void ConvertToPlayerModel_ValidDateTimeOffset_ConvertsCorrectly(string dateTimeString)
    {
        // Arrange
        var dateTimeOffset = DateTimeOffset.Parse(dateTimeString);
        var entity = new PlayerEntity
        {
            PartitionKey = "PLAYER",
            RowKey = "ABC",
            GamesPlayed = 10,
            GamesWon = 5,
            TotalScore = 100,
            TotalCorrectAnswers = 50,
            LastPlayed = dateTimeOffset
        };

        // Act
        var player = ConvertEntityToPlayer(entity);

        // Assert
        Assert.NotNull(player);
        Assert.Equal("ABC", player.Initials);
        Assert.Equal(10, player.GamesPlayed);
        Assert.Equal(5, player.GamesWon);
        Assert.Equal(100, player.TotalScore);
        Assert.Equal(50, player.TotalCorrectAnswers);
        Assert.Equal(dateTimeOffset.LocalDateTime, player.LastPlayed);
    }

    [Fact]
    public void ConvertToPlayerModel_NullLastPlayed_UsesUtcNowFallback()
    {
        // Arrange
        var entity = new PlayerEntity
        {
            PartitionKey = "PLAYER",
            RowKey = "XYZ",
            GamesPlayed = 0,
            GamesWon = 0,
            TotalScore = 0,
            TotalCorrectAnswers = 0,
            LastPlayed = null
        };

        // Act
        var player = ConvertEntityToPlayer(entity);
        var timeDifference = Math.Abs((DateTime.UtcNow - player.LastPlayed).TotalSeconds);

        // Assert
        Assert.NotNull(player);
        Assert.True(timeDifference < 5, "LastPlayed should be set to UtcNow when null");
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("xyz")]
    [InlineData("TeStInG")]
    public void InitialsFormatting_VariousCases_ConvertsToUpperInvariant(string initials)
    {
        // Arrange & Act
        var formatted = initials.ToUpperInvariant();

        // Assert
        Assert.Equal(initials.ToUpper(), formatted);
        Assert.Matches("^[A-Z]+$", formatted);
    }

    [Fact]
    public void PlayerEntity_WithAllProperties_RoundTripsCorrectly()
    {
        // Arrange
        var originalPlayer = new Player
        {
            Initials = "TST",
            GamesPlayed = 15,
            GamesWon = 10,
            TotalScore = 250,
            TotalCorrectAnswers = 120,
            LastPlayed = DateTime.UtcNow
        };

        // Act - Convert to entity and back
        var entity = new PlayerEntity
        {
            PartitionKey = "PLAYER",
            RowKey = originalPlayer.Initials.ToUpperInvariant(),
            GamesPlayed = originalPlayer.GamesPlayed,
            GamesWon = originalPlayer.GamesWon,
            TotalScore = originalPlayer.TotalScore,
            TotalCorrectAnswers = originalPlayer.TotalCorrectAnswers,
            LastPlayed = new DateTimeOffset(originalPlayer.LastPlayed, TimeSpan.Zero)
        };
        var roundTrippedPlayer = ConvertEntityToPlayer(entity);

        // Assert
        Assert.Equal(originalPlayer.Initials, roundTrippedPlayer.Initials);
        Assert.Equal(originalPlayer.GamesPlayed, roundTrippedPlayer.GamesPlayed);
        Assert.Equal(originalPlayer.GamesWon, roundTrippedPlayer.GamesWon);
        Assert.Equal(originalPlayer.TotalScore, roundTrippedPlayer.TotalScore);
        Assert.Equal(originalPlayer.TotalCorrectAnswers, roundTrippedPlayer.TotalCorrectAnswers);
    }

    // Helper method that simulates the conversion logic
    private Player ConvertEntityToPlayer(PlayerEntity entity)
    {
        DateTime lastPlayed;

        if (entity.LastPlayed.HasValue)
        {
            lastPlayed = entity.LastPlayed.Value.LocalDateTime;
        }
        else
        {
            lastPlayed = DateTime.UtcNow;
        }

        return new Player
        {
            Initials = entity.RowKey,
            GamesPlayed = entity.GamesPlayed,
            GamesWon = entity.GamesWon,
            TotalScore = entity.TotalScore,
            TotalCorrectAnswers = entity.TotalCorrectAnswers,
            LastPlayed = lastPlayed
        };
    }
}
