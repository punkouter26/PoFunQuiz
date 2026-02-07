using System;
using Azure;
using Azure.Data.Tables;

namespace PoFunQuiz.Web.Models;

/// <summary>
/// A leaderboard score entry persisted as an Azure Table Storage entity.
/// Implements ITableEntity for direct table operations (Active Record–style).
/// </summary>
public class LeaderboardEntry : ITableEntity
{
    public string PartitionKey { get; set; } = "Global";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxStreak { get; set; }
    public string Category { get; set; } = "General";
    public DateTime DatePlayed { get; set; } = DateTime.UtcNow;
    public int Wins { get; set; }
    public int Losses { get; set; }
}
