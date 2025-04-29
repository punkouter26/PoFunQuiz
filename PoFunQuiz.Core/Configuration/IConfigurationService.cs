namespace PoFunQuiz.Core.Configuration
{
    /// <summary>
    /// Interface for centralized configuration service
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the OpenAI service configuration
        /// </summary>
        OpenAISettings OpenAI { get; }
        
        /// <summary>
        /// Gets the Table Storage configuration
        /// </summary>
        TableStorageSettings TableStorage { get; }
    }
}