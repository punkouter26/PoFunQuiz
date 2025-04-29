using Microsoft.Extensions.Options;

namespace PoFunQuiz.Core.Configuration
{
    /// <summary>
    /// Centralized service for accessing application configuration
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IOptions<OpenAISettings> _openAIOptions;
        private readonly IOptions<TableStorageSettings> _tableStorageOptions;

        public ConfigurationService(
            IOptions<OpenAISettings> openAIOptions,
            IOptions<TableStorageSettings> tableStorageOptions)
        {
            _openAIOptions = openAIOptions;
            _tableStorageOptions = tableStorageOptions;
        }

        /// <summary>
        /// Gets the OpenAI service configuration
        /// </summary>
        public OpenAISettings OpenAI => _openAIOptions.Value;

        /// <summary>
        /// Gets the Table Storage configuration
        /// </summary>
        public TableStorageSettings TableStorage => _tableStorageOptions.Value;
    }
}