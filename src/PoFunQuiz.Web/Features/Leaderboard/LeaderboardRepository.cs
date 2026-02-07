using Azure;
using Azure.Data.Tables;
using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Leaderboard;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<LeaderboardRepository> _logger;

    public LeaderboardRepository(TableClient tableClient, ILogger<LeaderboardRepository> logger)
    {
        _logger = logger;
        _tableClient = tableClient;
        _tableClient.CreateIfNotExists();
    }

    public async Task AddScoreAsync(LeaderboardEntry entry)
    {
        try
        {
            await _tableClient.AddEntityAsync(entry);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error adding score to leaderboard");
            throw;
        }
    }

    public async Task<List<LeaderboardEntry>> GetTopScoresAsync(string category, int count = 10)
    {
        try
        {
            // Sanitize category to prevent OData filter injection (escape single quotes)
            var safeCategory = category.Replace("'", "''", StringComparison.Ordinal);
            var query = _tableClient.QueryAsync<LeaderboardEntry>(filter: $"PartitionKey eq '{safeCategory}'");
            var results = new List<LeaderboardEntry>();

            await foreach (var page in query.AsPages())
            {
                results.AddRange(page.Values);
            }

            return results
                .OrderByDescending(x => x.Score)
                .Take(count)
                .ToList();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error fetching leaderboard");
            return new List<LeaderboardEntry>();
        }
    }
}
