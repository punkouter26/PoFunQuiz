using FluentAssertions;
using PoFunQuiz.Web;
using PoFunQuiz.Web.Models;
using Xunit;

namespace PoFunQuiz.Tests.Unit;

/// <summary>
/// Unit tests for the R5 GameState encapsulation — SetGame/ClearGame/OnChange.
/// </summary>
public class GameStateTests
{
    private static GameSession MakeSession() => new GameSession
    {
        Player1 = new Player { Name = "P1", Initials = "P1" },
        Player2 = new Player { Name = "P2", Initials = "P2" }
    };

    [Fact]
    public void SetGame_ShouldPopulateCurrentGame()
    {
        var sut = new GameState();
        var session = MakeSession();

        sut.SetGame(session);

        sut.CurrentGame.Should().BeSameAs(session);
    }

    [Fact]
    public void ClearGame_ShouldNullCurrentGame()
    {
        var sut = new GameState();
        sut.SetGame(MakeSession());

        sut.ClearGame();

        sut.CurrentGame.Should().BeNull();
    }

    [Fact]
    public void SetGame_ShouldRaiseOnChange()
    {
        var sut = new GameState();
        int callCount = 0;
        sut.OnChange += () => callCount++;

        sut.SetGame(MakeSession());

        callCount.Should().Be(1);
    }

    [Fact]
    public void ClearGame_ShouldRaiseOnChange()
    {
        var sut = new GameState();
        sut.SetGame(MakeSession());
        int callCount = 0;
        sut.OnChange += () => callCount++;

        sut.ClearGame();

        callCount.Should().Be(1);
    }

    [Fact]
    public void SetGame_CalledTwice_RaisesOnChangeTwice()
    {
        var sut = new GameState();
        int callCount = 0;
        sut.OnChange += () => callCount++;

        sut.SetGame(MakeSession());
        sut.SetGame(MakeSession());

        callCount.Should().Be(2);
    }

    [Fact]
    public void CurrentGame_InitiallyNull()
    {
        var sut = new GameState();
        sut.CurrentGame.Should().BeNull();
    }
}
