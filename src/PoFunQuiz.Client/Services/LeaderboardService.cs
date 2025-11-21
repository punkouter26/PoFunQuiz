using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Client.Services
{
    public interface ILeaderboardService
    {
        Task<bool> SubmitScoreAsync(LeaderboardEntry entry);
    }

    public class LeaderboardService : ILeaderboardService
    {
        private readonly HttpClient _httpClient;

        public LeaderboardService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SubmitScoreAsync(LeaderboardEntry entry)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/leaderboard", entry);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
