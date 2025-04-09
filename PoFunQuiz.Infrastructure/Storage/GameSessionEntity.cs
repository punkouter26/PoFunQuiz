using System;
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
        // PartitionKey and RowKey are required by ITableEntity
        public string PartitionKey { get; set; } = "GameSession"; // Default PartitionKey
        public string RowKey { get; set; } = default!; // GameId

        // Timestamp and ETag are required by ITableEntity
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Game Session Data
        public string Player1Initials { get; set; } = default!;
        public string Player2Initials { get; set; } = default!;
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        // Note: Storing complex objects like Player or List<QuizQuestion> directly is not recommended.
        // Store identifiers (like Initials) or serialize if absolutely necessary (e.g., JSON string),
        // but be mindful of query limitations and storage costs.

        /// <summary>
        /// Parameterless constructor required for table storage deserialization.
        /// </summary>
        public GameSessionEntity() { }

        /// <summary>
        /// Creates a GameSessionEntity from a GameSession model.
        /// </summary>
        public static GameSessionEntity FromModel(GameSession model)
        {
            return new GameSessionEntity
            {
                RowKey = model.GameId,
                Player1Initials = model.Player1?.Initials ?? string.Empty,
                Player2Initials = model.Player2?.Initials ?? string.Empty,
                Player1Score = model.Player1Score,
                Player2Score = model.Player2Score,
                StartTime = model.StartTime,
                EndTime = model.EndTime
                // PartitionKey defaults to "GameSession"
            };
        }

        /// <summary>
        /// Converts this entity back to a GameSession model.
        /// Note: This requires fetching Player objects separately if needed.
        /// </summary>
        public GameSession ToModel(Player? player1 = null, Player? player2 = null)
        {
            return new GameSession
            {
                GameId = this.RowKey,
                Player1Initials = this.Player1Initials, // Store initials in model
                Player2Initials = this.Player2Initials, // Store initials in model
                Player1 = player1, // Assign if provided
                Player2 = player2, // Assign if provided
                Player1Score = this.Player1Score,
                Player2Score = this.Player2Score,
                StartTime = this.StartTime?.UtcDateTime,
                EndTime = this.EndTime?.UtcDateTime
            };
        }
    }
}
