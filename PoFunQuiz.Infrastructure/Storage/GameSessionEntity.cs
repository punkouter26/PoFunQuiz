using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.Data.Tables;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Infrastructure.Storage
{
    /// <summary>
    /// Represents a game session entity for Azure Table Storage.
    /// PartitionKey = "GameSession" (Could be date-based later for better querying)
    /// RowKey = GameId
    /// </summary>
    public class GameSessionEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "GameSession";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string GameId { get; set; } = string.Empty;
        public string Player1Id { get; set; } = string.Empty;
        public string Player2Id { get; set; } = string.Empty;
        public string Player1Initials { get; set; } = string.Empty;
        public string Player2Initials { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Player1BaseScore { get; set; }
        public int Player2BaseScore { get; set; }
        public int Player1StreakBonus { get; set; }
        public int Player2StreakBonus { get; set; }
        public int Player1SpeedBonus { get; set; }
        public int Player2SpeedBonus { get; set; }
        public int Player1TimeBonus { get; set; }
        public int Player2TimeBonus { get; set; }
        public string SelectedCategories { get; set; } = string.Empty;
        public string GameDifficulty { get; set; } = "Medium";

        /// <summary>
        /// Parameterless constructor required for table storage deserialization.
        /// </summary>
        public GameSessionEntity() { }

        /// <summary>
        /// Creates a GameSessionEntity from a GameSession model.
        /// </summary>
        public static GameSessionEntity FromModel(GameSession gameSession)
        {
            if (gameSession == null)
                throw new ArgumentNullException(nameof(gameSession));

            return new GameSessionEntity
            {
                PartitionKey = "GameSession",
                RowKey = gameSession.GameId,
                GameId = gameSession.GameId,
                Player1Id = gameSession.Player1?.Id ?? string.Empty,
                Player2Id = gameSession.Player2?.Id ?? string.Empty,
                Player1Initials = gameSession.Player1Initials,
                Player2Initials = gameSession.Player2Initials,
                StartTime = gameSession.StartTime,
                EndTime = gameSession.EndTime,
                Player1BaseScore = gameSession.Player1BaseScore,
                Player2BaseScore = gameSession.Player2BaseScore,
                Player1StreakBonus = gameSession.Player1StreakBonus,
                Player2StreakBonus = gameSession.Player2StreakBonus,
                Player1SpeedBonus = gameSession.Player1SpeedBonus,
                Player2SpeedBonus = gameSession.Player2SpeedBonus,
                Player1TimeBonus = gameSession.Player1TimeBonus,
                Player2TimeBonus = gameSession.Player2TimeBonus,
                SelectedCategories = string.Join(",", gameSession.SelectedCategories ?? new List<string>()),
                GameDifficulty = gameSession.GameDifficulty.ToString()
            };
        }

        /// <summary>
        /// Converts this entity to a GameSession model.
        /// Note: Player objects need to be fetched separately.
        /// </summary>
        public GameSession ToModel()
        {
            // Create placeholder Player objects with minimal information
            var player1 = new Player { Id = Player1Id, Initials = Player1Initials };
            var player2 = new Player { Id = Player2Id, Initials = Player2Initials };

            return new GameSession
            {
                GameId = GameId,
                Player1 = player1,
                Player2 = player2,
                Player1Initials = Player1Initials,
                Player2Initials = Player2Initials,
                StartTime = StartTime,
                EndTime = EndTime,
                Player1BaseScore = Player1BaseScore,
                Player2BaseScore = Player2BaseScore,
                Player1StreakBonus = Player1StreakBonus,
                Player2StreakBonus = Player2StreakBonus,
                Player1SpeedBonus = Player1SpeedBonus,
                Player2SpeedBonus = Player2SpeedBonus,
                Player1TimeBonus = Player1TimeBonus,
                Player2TimeBonus = Player2TimeBonus,
                SelectedCategories = SelectedCategories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                GameDifficulty = Enum.Parse<QuestionDifficulty>(GameDifficulty)
            };
        }
    }
}
