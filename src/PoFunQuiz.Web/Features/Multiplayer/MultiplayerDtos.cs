using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Multiplayer;

public class JoinGameDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
}

public class JoinGameResult
{
    public bool Success { get; set; }
    /// <summary>Empty on success. "not_found" | "already_started" | "already_full" on failure.</summary>
    public string FailReason { get; set; } = string.Empty;
}

/// <summary>Summary of a game waiting for a second player to join.</summary>
public class OpenGameInfo
{
    public string GameId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
}

public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public string CurrentQuestion { get; set; } = string.Empty;
    public bool IsGameStarted { get; set; }
    public bool IsGameOver { get; set; }
    /// <summary>Populated when the game starts, so each client can build a local GameSession.</summary>
    public List<QuizQuestion>? Questions { get; set; }
    public DateTime? StartTime { get; set; }
}
