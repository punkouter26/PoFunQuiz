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

            // Use maxPerPage to cap server-side rows read per page; collect only until we have
            // enough candidates to produce `count` sorted results without streaming the whole partition.
            // We request count * 3 candidates to sort accurately with minimal over-read.
            int candidateLimit = count * 3;
            var query = _tableClient.QueryAsync<LeaderboardEntry>(
                filter: $"PartitionKey eq '{safeCategory}'",
                maxPerPage: candidateLimit);
            var results = new List<LeaderboardEntry>(candidateLimit);

            await foreach (var page in query.AsPages())
            {
                results.AddRange(page.Values);
                // Stop reading additional pages once we have sufficient candidates
                if (results.Count >= candidateLimit) break;
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
