namespace PoFunQuiz.Shared.DTOs
{
    public class JoinGameDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
    }

    public class GameStateDto
    {
        public string GameId { get; set; } = string.Empty;
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        public string CurrentQuestion { get; set; } = string.Empty;
        public bool IsGameStarted { get; set; }
        public bool IsGameOver { get; set; }
    }
}
