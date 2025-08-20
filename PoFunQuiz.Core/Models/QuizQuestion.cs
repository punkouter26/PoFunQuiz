using System;
using System.Collections.Generic;

namespace PoFunQuiz.Core.Models
{
    public class QuizQuestion
    {
        // Question Identity
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Question Content
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }

        // Optional Properties
        public string Category { get; set; } = "General";
        public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

        // Helper properties
        public string CorrectAnswer => Options.Count > CorrectOptionIndex ? Options[CorrectOptionIndex] : string.Empty;
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

    public static class QuestionCategories
    {
        public static readonly string[] All = new[]
        {
            "General",
            "Science",
            "History",
            "Geography",
            "Technology",
            "Sports",
            "Entertainment",
            "Arts"
        };
    }
}