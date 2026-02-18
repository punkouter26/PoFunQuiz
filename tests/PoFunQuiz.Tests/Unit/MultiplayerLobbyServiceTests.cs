using Xunit;
using PoFunQuiz.Web.Features.Multiplayer;
using PoFunQuiz.Web.Models;
using FluentAssertions;

namespace PoFunQuiz.Tests.Unit
{
    public class MultiplayerLobbyServiceTests
    {
        private readonly MultiplayerLobbyService _sut;

        public MultiplayerLobbyServiceTests()
        {
            _sut = new MultiplayerLobbyService();
        }

        [Fact]
        public void CreateSession_ShouldReturnSessionWithValidIdAndPlayer1()
        {
            // Arrange
            var player1Name = "PlayerOne";

            // Act
            var session = _sut.CreateSession(player1Name, "conn-1");

            // Assert
            session.Should().NotBeNull();
            session.GameId.Should().NotBeNullOrEmpty();
            session.GameId.Length.Should().Be(6);
            session.Player1.Name.Should().Be(player1Name);
            session.Player1.Initials.Should().Be("PLA"); // First 3 chars upper
            session.Player2.Name.Should().Be("Waiting...");
        }

        [Fact]
        public void JoinSession_ShouldReturnTrue_WhenGameExistsAndIsNotFull()
        {
            // Arrange
            var session = _sut.CreateSession("P1", "conn-1");
            var player2Name = "PlayerTwo";

            // Act
            var result = _sut.JoinSession(session.GameId, player2Name, "conn-2");

            // Assert
            result.Should().BeTrue();
            var updatedSession = _sut.GetSession(session.GameId);
            updatedSession.Should().NotBeNull();
            updatedSession!.Player2.Name.Should().Be(player2Name);
            updatedSession.Player2.Initials.Should().Be("PLA");
        }

        [Fact]
        public void JoinSession_ShouldReturnFalse_WhenGameDoesNotExist()
        {
            // Act
            var result = _sut.JoinSession("INVALID", "P2", "conn-x");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void JoinSession_ShouldReturnFalse_WhenGameIsFull()
        {
            // Arrange
            var session = _sut.CreateSession("P1", "conn-1");
            _sut.JoinSession(session.GameId, "P2", "conn-2");

            // Act
            var result = _sut.JoinSession(session.GameId, "P3", "conn-3");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void RemoveSession_ShouldRemoveSession()
        {
            // Arrange
            var session = _sut.CreateSession("P1", "conn-1");

            // Act
            _sut.RemoveSession(session.GameId);

            // Assert
            var result = _sut.GetSession(session.GameId);
            result.Should().BeNull();
        }

        [Fact]
        public void MapToDto_ShouldMapCorrectly()
        {
            // Arrange
            var session = _sut.CreateSession("P1", "conn-1");
            _sut.JoinSession(session.GameId, "P2", "conn-2");
            session.Player1BaseScore = 10;
            session.Player2BaseScore = 20;
            session.StartTime = System.DateTime.UtcNow;

            // Act
            var dto = _sut.MapToDto(session);

            // Assert
            dto.GameId.Should().Be(session.GameId);
            dto.Player1Name.Should().Be("P1");
            dto.Player2Name.Should().Be("P2");
            dto.Player1Score.Should().Be(10);
            dto.Player2Score.Should().Be(20);
            dto.IsGameStarted.Should().BeTrue();
            dto.IsGameOver.Should().BeFalse();
        }
    }
}
