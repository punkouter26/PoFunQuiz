using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using PoFunQuiz.Web.Features.Multiplayer;
using Xunit;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Integration tests for GameHub covering L3 (host-only start),
/// L4 (join fail-reason via JoinGameResult DTO), and happy-path flow.
/// </summary>
public class GameHubAuthorizationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GameHubAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HubConnection BuildConnection()
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/gamehub"), o =>
            {
                o.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }

    // ── L3: only host can start ───────────────────────────────────────────────

    [Fact]
    public async Task StartGame_ByNonHost_ThrowsHubException()
    {
        var hostConn  = BuildConnection();
        var guestConn = BuildConnection();

        await hostConn.StartAsync();
        await guestConn.StartAsync();

        var gameId = await hostConn.InvokeAsync<string>("CreateGame", "Host");

        await guestConn.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = gameId, PlayerName = "Guest" });

        // Non-host attempting StartGame should throw HubException
        var act = async () => await guestConn.InvokeAsync("StartGame", gameId);
        await act.Should().ThrowAsync<HubException>();

        await hostConn.StopAsync();
        await guestConn.StopAsync();
    }

    [Fact]
    public async Task StartGame_ByHost_Succeeds()
    {
        var hostConn  = BuildConnection();
        var guestConn = BuildConnection();

        await hostConn.StartAsync();
        await guestConn.StartAsync();

        var gameId = await hostConn.InvokeAsync<string>("CreateGame", "Host2");
        await guestConn.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = gameId, PlayerName = "Guest2" });

        // Host should be able to start without exception
        var act = async () => await hostConn.InvokeAsync("StartGame", gameId);
        await act.Should().NotThrowAsync();

        await hostConn.StopAsync();
        await guestConn.StopAsync();
    }

    // ── L4: join fail reasons ─────────────────────────────────────────────────

    [Fact]
    public async Task JoinGame_UnknownGameId_ReturnsNotFound()
    {
        var conn = BuildConnection();
        await conn.StartAsync();

        var result = await conn.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = "ZZZZZZ", PlayerName = "P" });

        result.Success.Should().BeFalse();
        result.FailReason.Should().Be("not_found");

        await conn.StopAsync();
    }

    [Fact]
    public async Task JoinGame_AlreadyFull_ReturnsAlreadyFull()
    {
        var host  = BuildConnection();
        var guest = BuildConnection();
        var extra = BuildConnection();

        await host.StartAsync();
        await guest.StartAsync();
        await extra.StartAsync();

        var gameId = await host.InvokeAsync<string>("CreateGame", "HostFull");
        await guest.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = gameId, PlayerName = "GuestFull" });

        var result = await extra.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = gameId, PlayerName = "Crasher" });

        result.Success.Should().BeFalse();
        result.FailReason.Should().Be("already_full");

        await host.StopAsync();
        await guest.StopAsync();
        await extra.StopAsync();
    }

    [Fact]
    public async Task JoinGame_HappyPath_ReturnsSuccess()
    {
        var host  = BuildConnection();
        var guest = BuildConnection();

        await host.StartAsync();
        await guest.StartAsync();

        var gameId = await host.InvokeAsync<string>("CreateGame", "HostHappy");
        var result = await guest.InvokeAsync<JoinGameResult>("JoinGame",
            new JoinGameDto { GameId = gameId, PlayerName = "GuestHappy" });

        result.Success.Should().BeTrue();
        result.FailReason.Should().BeEmpty();

        await host.StopAsync();
        await guest.StopAsync();
    }
}
