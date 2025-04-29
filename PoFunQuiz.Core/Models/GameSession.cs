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
        public string Player1Initials { get; set; } = string.Empty; // Added for storage/retrieval
        public string Player2Initials { get; set; } = string.Empty; // Added for storage/retrieval
        
        // Questions
        public List<QuizQuestion> Player1Questions { get; set; } = new List<QuizQuestion>();
        public List<QuizQuestion> Player2Questions { get; set; } = new List<QuizQuestion>();
        
        // Game Timing
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue && StartTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : TimeSpan.Zero;
        
        // Enhanced Scoring
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
        
        // Total Scores (computed)
        public int Player1Score => Player1BaseScore + Player1StreakBonus + Player1SpeedBonus + Player1TimeBonus;
        public int Player2Score => Player2BaseScore + Player2StreakBonus + Player2SpeedBonus + Player2TimeBonus;
        
        // Selected Categories
        public List<string> SelectedCategories { get; set; } = new();
        public QuestionDifficulty GameDifficulty { get; set; } = QuestionDifficulty.Medium;
        
        // Game State
        public bool IsComplete => EndTime.HasValue;
        
        // Calculated Properties
        public Player? Winner => 
            Player1Score == Player2Score ? null : 
            Player1Score > Player2Score ? Player1 : Player2;
            
        public bool IsTie => Player1Score == Player2Score;

        // Scoring Methods
        public void AddAnswer(int playerNumber, bool isCorrect, int questionBasePoints, double speedMultiplier)
        {
            if (playerNumber == 1)
            {
                if (isCorrect)
                {
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

        private int CalculateStreakBonus(int streak)
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
}
