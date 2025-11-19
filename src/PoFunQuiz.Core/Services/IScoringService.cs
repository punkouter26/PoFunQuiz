using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Services
{
    public interface IScoringService
    {
        GameResult DetermineGameResult(GameSession gameSession);
        void UpdatePlayerStats(Player player, bool isWinner, GameSession gameSession);
    }

    public class GameResult
    {
        public bool IsTie { get; set; }
        public bool Player1Won { get; set; }
        public bool Player2Won { get; set; }
    }
}
