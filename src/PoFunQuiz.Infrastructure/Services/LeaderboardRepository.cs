using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Interfaces;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Infrastructure.Services
{
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
                // Note: Table Storage is not optimized for sorting by non-key fields.
                // For high scale, we would use a secondary index or Cosmos DB.
                // For this scale, client-side sorting or fetching by partition key is acceptable.

                var query = _tableClient.QueryAsync<LeaderboardEntry>(filter: $"PartitionKey eq '{category}'");
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
}
