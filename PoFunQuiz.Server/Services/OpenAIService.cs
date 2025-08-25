using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using PoFunQuiz.Core.Models; // Assuming QuizQuestion is in PoFunQuiz.Core.Models
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
        private readonly ChatClient _chatClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger; // Added logger

        public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger) // Injected logger
        {
            _configuration = configuration;
            _logger = logger; // Assigned logger
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];
            var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

            _logger.LogInformation("OpenAI Configuration - Endpoint: {Endpoint}, DeploymentName: {DeploymentName}", endpoint, deploymentName);

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
            {
                _logger.LogError("Azure OpenAI configuration is missing. Endpoint: {Endpoint}, ApiKey: {ApiKeyPresent}, DeploymentName: {DeploymentName}",
                    endpoint, !string.IsNullOrEmpty(apiKey), deploymentName);
                throw new ArgumentNullException("Azure OpenAI configuration is missing. Please check appsettings.json.");
            }

            Uri baseUri = new Uri(endpoint);
            _logger.LogInformation("Base URI: {BaseUri}, Host: {Host}", baseUri.ToString(), baseUri.Host);

            // If the endpoint is the generic Cognitive Services endpoint, append the deployment name
            if (baseUri.Host.Equals("eastus.api.cognitive.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                baseUri = new Uri(baseUri, $"openai/deployments/{deploymentName}");
                _logger.LogInformation("Modified URI for Cognitive Services: {BaseUri}", baseUri.ToString());
            }

            var azureClient = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(deploymentName);
        }
        public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
        {
            _logger.LogInformation("🔍 OPENAI: GenerateQuizQuestionsAsync called with topic='{Topic}', numberOfQuestions={Count}", topic, numberOfQuestions);

            var messages = new List<ChatMessage>
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

            var options = new ChatCompletionOptions
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

            try
            {
                _logger.LogInformation("🔍 OPENAI: Calling ChatClient.CompleteChatAsync...");
                ChatCompletion response = await _chatClient.CompleteChatAsync(messages, options);
                string jsonResponse = response.Content[0].Text;

                _logger.LogInformation("🔍 OPENAI: Raw response received, length: {Length} characters", jsonResponse?.Length ?? 0);
                _logger.LogInformation("🔍 OPENAI: Raw JSON response: {JsonResponse}", jsonResponse);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    _logger.LogWarning("🚨 OPENAI: OpenAI returned an empty response for topic '{Topic}'", topic);
                    return new List<QuizQuestion>();
                }

                // Try to deserialize with the new schema structure (questions wrapper)
                List<QuizQuestion>? questions = null;
                try
                {
                    _logger.LogInformation("🔍 OPENAI: Attempting to deserialize with schema wrapper...");
                    var schemaResponse = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(jsonResponse);
                    if (schemaResponse?.RootElement.TryGetProperty("questions", out var questionsProperty) == true)
                    {
                        questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(questionsProperty.GetRawText(),
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        _logger.LogInformation("🔍 OPENAI: Successfully deserialized schema wrapper with {Count} questions", questions?.Count ?? 0);
                    }
                    else
                    {
                        _logger.LogWarning("🚨 OPENAI: 'questions' property not found in schema response");
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning("🚨 OPENAI: Failed to deserialize with schema wrapper: {Error}", ex.Message);

                    // Fallback: Try to deserialize as a direct array
                    try
                    {
                        _logger.LogInformation("🔍 OPENAI: Attempting fallback to direct array deserialization...");
                        questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        _logger.LogInformation("🔍 OPENAI: Successfully deserialized as direct array with {Count} questions", questions?.Count ?? 0);
                    }
                    catch (System.Text.Json.JsonException innerEx)
                    {
                        // Last resort: Try to deserialize as a single object and wrap in a list
                        _logger.LogWarning("🚨 OPENAI: Failed to deserialize as direct array: {Error}", innerEx.Message);
                        _logger.LogInformation("🔍 OPENAI: Attempting to deserialize as single object...");
                        try
                        {
                            var singleQuestion = System.Text.Json.JsonSerializer.Deserialize<QuizQuestion>(jsonResponse, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (singleQuestion != null)
                            {
                                questions = new List<QuizQuestion> { singleQuestion };
                                _logger.LogInformation("🔍 OPENAI: Successfully wrapped single question in array");
                            }
                        }
                        catch (System.Text.Json.JsonException finalEx)
                        {
                            _logger.LogError(finalEx, "🚨 OPENAI: All deserialization attempts failed. Raw JSON: {JsonResponse}", jsonResponse);
                            return new List<QuizQuestion>();
                        }
                    }
                }

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("🚨 OPENAI: Deserialization resulted in no questions for topic '{Topic}'. Raw JSON: {JsonResponse}", topic, jsonResponse);
                }
                else
                {
                    _logger.LogInformation("🔍 OPENAI: Returning {Count} questions for topic '{Topic}'", questions.Count, topic);
                }

                return questions ?? new List<QuizQuestion>();
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error for OpenAI response. Topic: '{Topic}'.", topic);
                return new List<QuizQuestion>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz questions for topic '{Topic}'", topic);
                return new List<QuizQuestion>(); // Return empty list on error
            }
        }
    }
}
