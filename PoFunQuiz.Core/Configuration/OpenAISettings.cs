namespace PoFunQuiz.Core.Configuration
{
    /// <summary>
    /// Configuration settings for Azure OpenAI service
    /// </summary>
    public class OpenAISettings
    {
        public required string Endpoint { get; set; }
        public required string Key { get; set; }
        public required string DeploymentName { get; set; } = "gpt-3.5-turbo";
    }
} 