namespace PoFunQuiz.Core.Configuration
{
    /// <summary>
    /// Configuration settings for Azure Table Storage
    /// </summary>
    public class TableStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TablePrefix { get; set; } = "quiz";
        public string GameSessionTableName => $"{TablePrefix}gamesessions";
        public string PlayerTableName => $"{TablePrefix}players";
        public string QuestionTableName => $"{TablePrefix}questions";
    }
}