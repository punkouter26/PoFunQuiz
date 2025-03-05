using System;
using System.Collections.Generic;

namespace PoFunQuiz.Core.Models
{
    public class QuizQuestion
    {
        // Question Identity
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Question Content
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }
        
        // Optional Properties
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Medium";
        
        // Helper properties
        public string CorrectAnswer => Options.Count > CorrectOptionIndex ? Options[CorrectOptionIndex] : string.Empty;
    }
}