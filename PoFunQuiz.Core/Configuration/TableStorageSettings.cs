namespace PoFunQuiz.Core.Configuration
{
    /// <summary>
    /// Configuration settings for Azure Table Storage
    /// </summary>
    public class TableStorageSettings
    {
        public required string ConnectionString { get; set; }
        public string TableName { get; set; } = "PlayerStats";
    }
}