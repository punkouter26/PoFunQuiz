using FluentAssertions;
using PoFunQuiz.Web.Models;
using Xunit;

namespace PoFunQuiz.Tests.Unit;

/// <summary>
/// Tests for GameSession domain logic — AddAnswer per-player isolation (L1),
/// scoring computation, CalculateTimeBonus edge-cases, Winner/IsTie, and EndTime stamps.
/// </summary>
public class GameSessionTests
{
    private static GameSession MakeSession() => new GameSession
    {
        Player1 = new Player { Name = "Alice", Initials = "ALI" },
        Player2 = new Player { Name = "Bob",   Initials = "BOB" }
    };

    // ── Per-player score independence (L1 domain guard) ───────────────────────

    [Fact]
    public void AddAnswer_Player1Correct_DoesNotChangePlayer2Score()
    {
        var s = MakeSession();
        s.AddAnswer(1, isCorrect: true, questionBasePoints: 100, speedMultiplier: 1.0);
        s.Player2BaseScore.Should().Be(0);
        s.Player2Streak.Should().Be(0);
    }

    [Fact]
    public void AddAnswer_Player2Correct_DoesNotChangePlayer1Score()
    {
        var s = MakeSession();
        s.AddAnswer(2, isCorrect: true, questionBasePoints: 100, speedMultiplier: 1.0);
        s.Player1BaseScore.Should().Be(0);
        s.Player1Streak.Should().Be(0);
    }

    // ── Streak logic ──────────────────────────────────────────────────────────

    [Fact]
    public void AddAnswer_CorrectAnswers_IncrementStreak()
    {
        var s = MakeSession();
        s.AddAnswer(1, true, 10, 1.0);
        s.AddAnswer(1, true, 10, 1.0);
        s.Player1Streak.Should().Be(2);
        s.Player1MaxStreak.Should().Be(2);
    }

    [Fact]
    public void AddAnswer_IncorrectAnswer_ResetsStreak()
    {
        var s = MakeSession();
        s.AddAnswer(1, true, 10, 1.0);
        s.AddAnswer(1, true, 10, 1.0);
        s.AddAnswer(1, false, 10, 1.0);
        s.Player1Streak.Should().Be(0);
        s.Player1MaxStreak.Should().Be(2); // max preserved
    }

    // ── Speed bonus ───────────────────────────────────────────────────────────

    [Fact]
    public void AddAnswer_SpeedMultiplier2x_AddsSpeedBonus()
    {
        var s = MakeSession();
        s.AddAnswer(1, true, 100, speedMultiplier: 2.0);
        s.Player1SpeedBonus.Should().Be(100); // 100 * (2.0-1) = 100
        s.Player1BaseScore.Should().Be(100);
    }

    [Fact]
    public void AddAnswer_SpeedMultiplier1x_NoSpeedBonus()
    {
        var s = MakeSession();
        s.AddAnswer(1, true, 100, speedMultiplier: 1.0);
        s.Player1SpeedBonus.Should().Be(0);
    }

    // ── Total score composite ─────────────────────────────────────────────────

    [Fact]
    public void Player1Score_IsSum_Of_Base_Streak_Speed_TimeBonus()
    {
        var s = MakeSession();
        s.Player1BaseScore  = 50;
        s.Player1StreakBonus = 5;
        s.Player1SpeedBonus = 10;
        s.Player1TimeBonus  = 3;
        s.Player1Score.Should().Be(68);
    }

    // ── Winner / IsTie ────────────────────────────────────────────────────────

    [Fact]
    public void Winner_ReturnsPlayer1_WhenPlayer1ScoreHigher()
    {
        var s = MakeSession();
        s.Player1BaseScore = 100;
        s.Player2BaseScore = 50;
        s.Winner.Should().BeSameAs(s.Player1);
        s.IsTie.Should().BeFalse();
    }

    [Fact]
    public void Winner_ReturnsPlayer2_WhenPlayer2ScoreHigher()
    {
        var s = MakeSession();
        s.Player1BaseScore = 10;
        s.Player2BaseScore = 20;
        s.Winner.Should().BeSameAs(s.Player2);
    }

    [Fact]
    public void Winner_ReturnsNull_OnTie()
    {
        var s = MakeSession();
        s.Player1BaseScore = 50;
        s.Player2BaseScore = 50;
        s.Winner.Should().BeNull();
        s.IsTie.Should().BeTrue();
    }

    // ── IsComplete ────────────────────────────────────────────────────────────

    [Fact]
    public void IsComplete_FalseWhenEndTimeNull()
    {
        var s = MakeSession();
        s.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_TrueWhenEndTimeSet()
    {
        var s = MakeSession();
        s.EndTime = DateTime.UtcNow;
        s.IsComplete.Should().BeTrue();
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    [Fact]
    public void Duration_IsZero_WhenEndTimeNull()
    {
        var s = MakeSession();
        s.StartTime = DateTime.UtcNow;
        s.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_CorrectlyCalculated()
    {
        var s = MakeSession();
        s.StartTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        s.EndTime   = new DateTime(2026, 1, 1, 12, 1, 30, DateTimeKind.Utc);
        s.Duration.Should().Be(TimeSpan.FromSeconds(90));
    }

    // ── Invalid playerNumber guard ────────────────────────────────────────────

    [Fact]
    public void AddAnswer_InvalidPlayerNumber_ThrowsArgumentOutOfRange()
    {
        var s = MakeSession();
        var act = () => s.AddAnswer(3, true, 10, 1.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
