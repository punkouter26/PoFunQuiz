using System.Collections.Generic;
using System.Threading.Tasks;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Interfaces
{
    public interface ILeaderboardRepository
    {
        Task<List<LeaderboardEntry>> GetTopScoresAsync(string category, int count = 10);
        Task AddScoreAsync(LeaderboardEntry entry);
    }
}
