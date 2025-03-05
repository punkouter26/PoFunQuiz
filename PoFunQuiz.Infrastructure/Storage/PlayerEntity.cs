using System;
using Azure;
using Azure.Data.Tables;

namespace PoFunQuiz.Infrastructure.Storage
{
    /// <summary>
    /// Entity class for storing player data in Azure Table Storage
    /// </summary>
    public class PlayerEntity : ITableEntity
    {
        public required string PartitionKey { get; set; }
        public required string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        
        // Player statistics
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int TotalScore { get; set; }
        public int TotalCorrectAnswers { get; set; }
        
        // Changed from DateTime to object to handle any format that Azure Table Storage might return
        public object LastPlayed { get; set; }
    }
}