using System;
using Azure;
using Azure.Data.Tables;

namespace PoFunQuiz.Core.Models
{
    public class LeaderboardEntry : ITableEntity
    {
        // PartitionKey will be "Global" or "CategoryName"
        public string PartitionKey { get; set; } = "Global";

        // RowKey will be a unique ID (Guid)
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxStreak { get; set; }
        public string Category { get; set; } = "General";
        public DateTime DatePlayed { get; set; } = DateTime.UtcNow;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
    }
}
