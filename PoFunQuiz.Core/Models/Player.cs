using System;
using System.Collections.Generic;

namespace PoFunQuiz.Core.Models
{
    public class Player
    {
        // Player identity
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Initials { get; set; } = string.Empty;
        
        // Game statistics
        public int GamesPlayed { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        public int TotalScore { get; set; } = 0;
        public int TotalCorrectAnswers { get; set; } = 0;
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
        
        // Calculated statistics (not stored)
        public int Rank { get; set; } = 0;
        
        // Calculated properties
        public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed : 0;
        public double AverageScore => GamesPlayed > 0 ? (double)TotalScore / GamesPlayed : 0;
        public double Accuracy => GamesPlayed > 0 ? (double)TotalCorrectAnswers / (GamesPlayed * 10) : 0;
    }
}