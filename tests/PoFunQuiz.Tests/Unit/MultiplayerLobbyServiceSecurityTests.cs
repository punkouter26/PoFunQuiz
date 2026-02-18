using FluentAssertions;
using PoFunQuiz.Web.Features.Multiplayer;
using Xunit;

namespace PoFunQuiz.Tests.Unit;

/// <summary>
/// Covers the new IsHost authorization, TryJoinSession fail-reasons,
/// PurgeExpiredSessions, and the pre-existing join/session lifecycle — post R3/L3/L4 refactor.
/// </summary>
public class MultiplayerLobbyServiceSecurityTests
{
    private readonly MultiplayerLobbyService _sut = new();

    // ── IsHost authorization (L3) ─────────────────────────────────────────────

    [Fact]
    public void IsHost_ReturnsTrueForCreatingConnection()
    {
        var session = _sut.CreateSession("Host", "conn-host");
        _sut.IsHost(session.GameId, "conn-host").Should().BeTrue();
    }

    [Fact]
    public void IsHost_ReturnsFalseForDifferentConnection()
    {
        var session = _sut.CreateSession("Host", "conn-host");
        _sut.IsHost(session.GameId, "conn-intruder").Should().BeFalse();
    }

    [Fact]
    public void IsHost_ReturnsFalseForPlayer2Connection()
    {
        var session = _sut.CreateSession("Host", "conn-host");
        _sut.TryJoinSession(session.GameId, "Guest", "conn-guest", out _);
        _sut.IsHost(session.GameId, "conn-guest").Should().BeFalse();
    }

    [Fact]
    public void IsHost_ReturnsFalseForUnknownGameId()
    {
        _sut.IsHost("NOEXIST", "conn-x").Should().BeFalse();
    }

    // ── TryJoinSession fail reasons (L4) ──────────────────────────────────────

    [Fact]
    public void TryJoinSession_ReturnsNotFound_WhenGameIdMissing()
    {
        var success = _sut.TryJoinSession("BADID", "P2", "conn-2", out var reason);
        success.Should().BeFalse();
        reason.Should().Be(JoinFailReason.NotFound);
    }

    [Fact]
    public void TryJoinSession_ReturnsAlreadyFull_WhenP2AlreadyJoined()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        _sut.TryJoinSession(session.GameId, "P2", "conn-2", out _);

        var success = _sut.TryJoinSession(session.GameId, "P3", "conn-3", out var reason);
        success.Should().BeFalse();
        reason.Should().Be(JoinFailReason.AlreadyFull);
    }

    [Fact]
    public void TryJoinSession_ReturnsAlreadyStarted_WhenGameHasBegun()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        _sut.TryJoinSession(session.GameId, "P2", "conn-2", out _);
        session.StartTime = DateTime.UtcNow; // simulate game started

        var success = _sut.TryJoinSession(session.GameId, "LateJoiner", "conn-late", out var reason);
        success.Should().BeFalse();
        reason.Should().Be(JoinFailReason.AlreadyStarted);
    }

    [Fact]
    public void TryJoinSession_ReturnsNone_WhenSuccessful()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        var success = _sut.TryJoinSession(session.GameId, "P2", "conn-2", out var reason);
        success.Should().BeTrue();
        reason.Should().Be(JoinFailReason.None);
    }

    // ── PurgeExpiredSessions (R3) ──────────────────────────────────────────────

    [Fact]
    public void PurgeExpiredSessions_RemovesCompletedOldSessions()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        _sut.TryJoinSession(session.GameId, "P2", "conn-2", out _);
        session.StartTime = DateTime.UtcNow.AddHours(-3);
        session.EndTime   = DateTime.UtcNow.AddHours(-3);

        _sut.PurgeExpiredSessions(TimeSpan.FromHours(2));

        _sut.GetSession(session.GameId).Should().BeNull();
    }

    [Fact]
    public void PurgeExpiredSessions_KeepsRecentSessions()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        session.StartTime = DateTime.UtcNow.AddMinutes(-5);
        session.EndTime   = DateTime.UtcNow.AddMinutes(-5);

        _sut.PurgeExpiredSessions(TimeSpan.FromHours(2));

        _sut.GetSession(session.GameId).Should().NotBeNull();
    }

    [Fact]
    public void PurgeExpiredSessions_RemovesStartedButAbandonedSessions()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        session.StartTime = DateTime.UtcNow.AddHours(-5); // started long ago, never ended

        _sut.PurgeExpiredSessions(TimeSpan.FromHours(2));

        _sut.GetSession(session.GameId).Should().BeNull();
    }

    // ── OnDisconnected cleans host tracking (R3 host map) ─────────────────────

    [Fact]
    public void OnDisconnected_BeforeStart_RemovesSessionAndHostEntry()
    {
        var session = _sut.CreateSession("P1", "conn-1");
        var gameId = session.GameId;

        _sut.OnDisconnected("conn-1");

        _sut.GetSession(gameId).Should().BeNull();
        _sut.IsHost(gameId, "conn-1").Should().BeFalse();
    }
}
