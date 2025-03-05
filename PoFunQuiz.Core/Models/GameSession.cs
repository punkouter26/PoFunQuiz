using System;
using System.Collections.Generic;

namespace PoFunQuiz.Core.Models
{
    public class GameSession
    {
        // Game Identity
        public string GameId { get; set; } = Guid.NewGuid().ToString();
        
        // Players
        public required Player Player1 { get; set; }
        public required Player Player2 { get; set; }
        
        // Questions
        public List<QuizQuestion> Player1Questions { get; set; } = new List<QuizQuestion>();
        public List<QuizQuestion> Player2Questions { get; set; } = new List<QuizQuestion>();
        
        // Game Timing
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue && StartTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : TimeSpan.Zero;
        
        // Game Results
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        
        // Game State
        public bool IsComplete => EndTime.HasValue;
        
        // Calculated Properties
        public Player? Winner => 
            Player1Score == Player2Score ? null : 
            Player1Score > Player2Score ? Player1 : Player2;
            
        public bool IsTie => Player1Score == Player2Score;
    }
}