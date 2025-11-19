using System;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Services
{
    public class ScoringService : IScoringService
    {
        public GameResult DetermineGameResult(GameSession gameSession)
        {
            if (gameSession == null) throw new ArgumentNullException(nameof(gameSession));

            bool isTie = gameSession.Player1Score == gameSession.Player2Score;
            bool player1Won = gameSession.Player1Score > gameSession.Player2Score;

            return new GameResult
            {
                IsTie = isTie,
                Player1Won = player1Won,
                Player2Won = !isTie && !player1Won
            };
        }

        public void UpdatePlayerStats(Player player, bool isWinner, GameSession gameSession)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (gameSession == null) throw new ArgumentNullException(nameof(gameSession));

            // Calculate player stats from game session
            int playerScore = player.Initials == gameSession.Player1Initials ? gameSession.Player1Score : gameSession.Player2Score;
            int correctAnswers = player.Initials == gameSession.Player1Initials ? gameSession.Player1CorrectCount : gameSession.Player2CorrectCount;

            // Update player stats using domain logic
            player.UpdateStats(playerScore, correctAnswers, isWinner);
        }
    }
}
