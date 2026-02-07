using System;

namespace PoFunQuiz.Web.Models;

/// <summary>
/// Represents a quiz player with statistics and scoring history.
/// </summary>
public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;

    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int TotalScore { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public DateTime LastPlayed { get; set; } = DateTime.UtcNow;

    public int Rank { get; set; }

    // Computed statistics
    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed : 0;
    public double AverageScore => GamesPlayed > 0 ? (double)TotalScore / GamesPlayed : 0;
    public double Accuracy => GamesPlayed > 0 ? (double)TotalCorrectAnswers / (GamesPlayed * 10) : 0;

    /// <summary>
    /// Updates aggregate stats after a game (SRP — single mutation point for player state).
    /// </summary>
    public void UpdateStats(int score, int correctAnswers, bool isWinner)
    {
        GamesPlayed++;
        if (isWinner) GamesWon++;
        TotalScore += score;
        TotalCorrectAnswers += correctAnswers;
        LastPlayed = DateTime.UtcNow;
    }
}
