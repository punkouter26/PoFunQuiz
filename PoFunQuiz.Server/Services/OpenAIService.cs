using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using PoFunQuiz.Core.Models; // Assuming QuizQuestion is in PoFunQuiz.Core.Models
using System;
using OpenAI.Chat;
using Microsoft.Extensions.Logging; // Added for logging

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

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
            {
                _logger.LogError("Azure OpenAI configuration is missing. Endpoint: {Endpoint}, ApiKey: {ApiKeyPresent}, DeploymentName: {DeploymentName}",
                    endpoint, !string.IsNullOrEmpty(apiKey), deploymentName);
                throw new ArgumentNullException("Azure OpenAI configuration is missing. Please check appsettings.json.");
            }

            Uri baseUri = new Uri(endpoint);
            // If the endpoint is the generic Cognitive Services endpoint, append the deployment name
            if (baseUri.Host.Equals("eastus.api.cognitive.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                baseUri = new Uri(baseUri, $"openai/deployments/{deploymentName}");
            }

            var azureClient = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(deploymentName);
        }
        public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($@"You are a helpful assistant designed to output JSON.
                    Generate {numberOfQuestions} multiple-choice quiz questions about {topic}.
                    Each question should have a 'Question' (string), 'Options' (an array of strings), and a 'CorrectOptionIndex' (integer, 0-based index of the correct option in the 'Options' array).
                    Ensure the JSON is valid and follows this structure:
                    [
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
                    ]"),
                new UserChatMessage($"Generate {numberOfQuestions} questions about {topic}.")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            try
            {
                ChatCompletion response = await _chatClient.CompleteChatAsync(messages, options);
                string jsonResponse = response.Content[0].Text;

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    _logger.LogWarning("OpenAI returned an empty response for topic '{Topic}'", topic);
                    return new List<QuizQuestion>();
                }

                _logger.LogInformation("Raw OpenAI JSON response: {JsonResponse}", jsonResponse);

                // Deserialize the JSON response into a list of QuizQuestion objects
                var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("Deserialization resulted in no questions for topic '{Topic}'. Raw JSON: {JsonResponse}", topic, jsonResponse);
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
