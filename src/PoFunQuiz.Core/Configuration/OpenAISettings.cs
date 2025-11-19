namespace PoFunQuiz.Core.Configuration;

/// <summary>
/// Configuration settings for Azure OpenAI service
/// </summary>
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2000;
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}
