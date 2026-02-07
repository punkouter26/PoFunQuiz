using PoFunQuiz.Web.Models;

namespace PoFunQuiz.Web.Features.Leaderboard;

public interface ILeaderboardRepository
{
    Task<List<LeaderboardEntry>> GetTopScoresAsync(string category, int count = 10);
    Task AddScoreAsync(LeaderboardEntry entry);
}
