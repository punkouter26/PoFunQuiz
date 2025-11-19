using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Collections.Generic;
using PoFunQuiz.Core.Models; // Assuming QuizQuestion is in PoFunQuiz.Core.Models
using PoFunQuiz.Core.Configuration;
using System;
using OpenAI.Chat;
using Microsoft.Extensions.Logging; // Added for logging
using System.Text.Json;
using System.Linq;

namespace PoFunQuiz.Server.Services
{
    // Using Dependency Inversion Principle (DIP) - High-level modules (e.g., controllers) should not depend on low-level modules (e.g., OpenAIService implementation).
    // Instead, both should depend on abstractions (IOpenAIService).
    public interface IOpenAIService
    {
        Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions);
    }

    public class OpenAIService : IOpenAIService
    {
        private ChatClient _chatClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly OpenAISettings _settings;
        private readonly IEnumerable<IQuizQuestionDeserializer> _deserializers;

        public OpenAIService(
            IOptions<AppSettings> appSettings,
            IConfiguration configuration,
            ILogger<OpenAIService> logger,
            IEnumerable<IQuizQuestionDeserializer> deserializers)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = appSettings.Value.AzureOpenAI;
            _deserializers = deserializers;

            _chatClient = InitializeChatClient();
        }

        private ChatClient InitializeChatClient()
        {
            var endpoint = _settings.Endpoint;
            var apiKey = _settings.ApiKey;
            var deploymentName = _settings.DeploymentName;

            _logger.LogInformation("OpenAI Configuration - Endpoint: {Endpoint}, DeploymentName: {DeploymentName}", endpoint, deploymentName);

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
            {
                _logger.LogError("Azure OpenAI configuration is missing. Endpoint: {Endpoint}, ApiKey: {ApiKeyPresent}, DeploymentName: {DeploymentName}",
                    endpoint, !string.IsNullOrEmpty(apiKey), deploymentName);
                throw new ArgumentNullException("Azure OpenAI configuration is missing. Please check appsettings.json.");
            }

            var baseUri = BuildEndpointUri(endpoint, deploymentName);
            var azureClient = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
            return azureClient.GetChatClient(deploymentName);
        }

        private Uri BuildEndpointUri(string endpoint, string deploymentName)
        {
            Uri baseUri = new Uri(endpoint);
            _logger.LogInformation("Base URI: {BaseUri}, Host: {Host}", baseUri.ToString(), baseUri.Host);

            // If the endpoint is the generic Cognitive Services endpoint, append the deployment name
            if (baseUri.Host.Equals("eastus.api.cognitive.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                baseUri = new Uri(baseUri, $"openai/deployments/{deploymentName}");
                _logger.LogInformation("Modified URI for Cognitive Services: {BaseUri}", baseUri.ToString());
            }

            return baseUri;
        }
        public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
        {
            _logger.LogInformation("Generating {Count} quiz questions for topic '{Topic}'", numberOfQuestions, topic);

            try
            {
                var jsonResponse = await FetchOpenAIResponse(topic, numberOfQuestions);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    _logger.LogWarning("OpenAI returned an empty response for topic '{Topic}'", topic);
                    return new List<QuizQuestion>();
                }

                return DeserializeQuestions(jsonResponse, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz questions for topic '{Topic}'", topic);
                return new List<QuizQuestion>();
            }
        }

        private async Task<string> FetchOpenAIResponse(string topic, int numberOfQuestions)
        {
            var messages = BuildChatMessages(topic, numberOfQuestions);
            var options = BuildChatOptions();

            _logger.LogInformation("Calling ChatClient.CompleteChatAsync...");
            ChatCompletion response = await _chatClient.CompleteChatAsync(messages, options);
            string jsonResponse = response.Content[0].Text ?? string.Empty;

            _logger.LogInformation("Received response: {Length} characters", jsonResponse.Length);
            _logger.LogDebug("Raw JSON response: {JsonResponse}", jsonResponse);

            return jsonResponse;
        }

        private List<ChatMessage> BuildChatMessages(string topic, int numberOfQuestions)
        {
            return new List<ChatMessage>
            {
                new SystemChatMessage($@"You are a helpful assistant designed to output JSON.
                    Generate {numberOfQuestions} multiple-choice quiz questions about {topic}.
                    Each question should have a 'Question' (string), 'Options' (an array of exactly 4 strings), and a 'CorrectOptionIndex' (integer, 0-based index of the correct option in the 'Options' array).
                    IMPORTANT: Return a JSON object with a 'questions' property containing an array of question objects.
                    Ensure the JSON follows this exact structure:
                    {{
                        ""questions"": [
                            {{
                                ""Question"": ""What is the capital of France?"",
                                ""Options"": [""Berlin"", ""Madrid"", ""Paris"", ""Rome""],
                                ""CorrectOptionIndex"": 2
                            }},
                            {{
                                ""Question"": ""Which planet is known as the Red Planet?"",
                                ""Options"": [""Earth"", ""Mars"", ""Jupiter"", ""Venus""],
                                ""CorrectOptionIndex"": 1
                            }}
                        ]
                    }}"),
                new UserChatMessage($"Generate exactly {numberOfQuestions} questions about {topic}. Return as a JSON array.")
            };
        }

        private ChatCompletionOptions BuildChatOptions()
        {
            return new ChatCompletionOptions
            {
                Temperature = 0.7f,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "quiz_questions",
                    jsonSchema: BinaryData.FromBytes(
                        """
                        {
                            "type": "object",
                            "properties": {
                                "questions": {
                                    "type": "array",
                                    "items": {
                                        "type": "object",
                                        "properties": {
                                            "Question": { "type": "string" },
                                            "Options": {
                                                "type": "array",
                                                "items": { "type": "string" },
                                                "minItems": 4,
                                                "maxItems": 4
                                            },
                                            "CorrectOptionIndex": { 
                                                "type": "integer", 
                                                "minimum": 0, 
                                                "maximum": 3 
                                            }
                                        },
                                        "required": ["Question", "Options", "CorrectOptionIndex"],
                                        "additionalProperties": false
                                    }
                                }
                            },
                            "required": ["questions"],
                            "additionalProperties": false
                        }
                        """u8.ToArray()),
                    jsonSchemaIsStrict: true)
            };
        }

        private List<QuizQuestion> DeserializeQuestions(string json, string topic)
        {
            _logger.LogInformation("Attempting to deserialize questions using Chain of Responsibility pattern");

            foreach (var deserializer in _deserializers)
            {
                if (deserializer.TryDeserialize(json, out var questions))
                {
                    _logger.LogInformation("Successfully deserialized {Count} questions for topic '{Topic}'", questions.Count, topic);
                    return questions;
                }
            }

            _logger.LogError("All deserialization attempts failed for topic '{Topic}'. Raw JSON: {Json}", topic, json);
            return new List<QuizQuestion>();
        }
    }
}
