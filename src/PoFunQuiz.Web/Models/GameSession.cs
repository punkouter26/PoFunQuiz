using System;
using System.Collections.Generic;

namespace PoFunQuiz.Web.Models;

/// <summary>
/// Encapsulates the full state of a two-player quiz game session including scoring,
/// streaks, and time bonuses. Immutable result determination via computed properties.
/// </summary>
public class GameSession
{
    public string GameId { get; set; } = Guid.NewGuid().ToString();

    public required Player Player1 { get; set; }
    public required Player Player2 { get; set; }

    public List<QuizQuestion> Player1Questions { get; set; } = [];
    public List<QuizQuestion> Player2Questions { get; set; } = [];

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue && StartTime.HasValue
        ? EndTime.Value - StartTime.Value
        : TimeSpan.Zero;

    // Scoring breakdown
    public int Player1BaseScore { get; set; }
    public int Player2BaseScore { get; set; }
    public int Player1StreakBonus { get; set; }
    public int Player2StreakBonus { get; set; }
    public int Player1SpeedBonus { get; set; }
    public int Player2SpeedBonus { get; set; }
    public int Player1TimeBonus { get; set; }
    public int Player2TimeBonus { get; set; }
    public int Player1Streak { get; set; }
    public int Player2Streak { get; set; }
    public int Player1MaxStreak { get; set; }
    public int Player2MaxStreak { get; set; }
    public int Player1CorrectCount { get; set; }
    public int Player2CorrectCount { get; set; }

    // Computed totals (Open/Closed principle — add new bonus types without modifying callers)
    public int Player1Score => Player1BaseScore + Player1StreakBonus + Player1SpeedBonus + Player1TimeBonus;
    public int Player2Score => Player2BaseScore + Player2StreakBonus + Player2SpeedBonus + Player2TimeBonus;

    public List<string> SelectedCategories { get; set; } = new();
    public QuestionDifficulty GameDifficulty { get; set; } = QuestionDifficulty.Medium;

    public bool IsComplete => EndTime.HasValue;

    public Player? Winner =>
        Player1Score == Player2Score ? null :
        Player1Score > Player2Score ? Player1 : Player2;

    public bool IsTie => Player1Score == Player2Score;

    /// <summary>
    /// Records a player's answer and updates scoring state (Command pattern — single mutation method).
    /// </summary>
    public void AddAnswer(int playerNumber, bool isCorrect, int questionBasePoints, double speedMultiplier)
    {
        if (playerNumber is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(playerNumber), playerNumber, "Player number must be 1 or 2.");
        }

        if (playerNumber == 1)
        {
            if (isCorrect)
            {
                Player1CorrectCount++;
                Player1Streak++;
                Player1MaxStreak = Math.Max(Player1MaxStreak, Player1Streak);
                Player1BaseScore += questionBasePoints;
                Player1SpeedBonus += (int)(questionBasePoints * (speedMultiplier - 1));
                Player1StreakBonus += CalculateStreakBonus(Player1Streak);
            }
            else
            {
                Player1Streak = 0;
            }
        }
        else
        {
            if (isCorrect)
            {
                Player2CorrectCount++;
                Player2Streak++;
                Player2MaxStreak = Math.Max(Player2MaxStreak, Player2Streak);
                Player2BaseScore += questionBasePoints;
                Player2SpeedBonus += (int)(questionBasePoints * (speedMultiplier - 1));
                Player2StreakBonus += CalculateStreakBonus(Player2Streak);
            }
            else
            {
                Player2Streak = 0;
            }
        }
    }

    private static int CalculateStreakBonus(int streak)
    {
        return streak switch
        {
            >= 5 => 3,
            >= 3 => 2,
            >= 2 => 1,
            _ => 0
        };
    }
}
