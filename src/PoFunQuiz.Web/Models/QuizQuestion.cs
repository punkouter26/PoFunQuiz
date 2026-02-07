using System;
using System.Collections.Generic;

namespace PoFunQuiz.Web.Models;

/// <summary>
/// A single multiple-choice quiz question with difficulty-based scoring.
/// </summary>
public class QuizQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; }

    public string Category { get; set; } = "General";
    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    public string CorrectAnswer => CorrectOptionIndex >= 0 && CorrectOptionIndex < Options.Count
        ? Options[CorrectOptionIndex]
        : string.Empty;

    /// <summary>
    /// Points awarded per correct answer, scaled by difficulty (Strategy pattern for scoring).
    /// </summary>
    public int BasePoints => Difficulty switch
    {
        QuestionDifficulty.Easy => 1,
        QuestionDifficulty.Medium => 2,
        QuestionDifficulty.Hard => 3,
        _ => 1
    };
}

public enum QuestionDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Central registry of available quiz categories.
/// </summary>
public static class QuestionCategories
{
    public static readonly string[] All =
    [
        "General",
        "Science",
        "History",
        "Geography",
        "Technology",
        "Sports",
        "Entertainment",
        "Arts"
    ];
}
