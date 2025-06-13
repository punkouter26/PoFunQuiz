using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using PoFunQuiz.Core.Models; // Assuming QuizQuestion is in PoFunQuiz.Core.Models
using System;
using OpenAI.Chat;

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

        public OpenAIService(IConfiguration configuration)
        {
            _configuration = configuration;
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];
            var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
            {
                throw new ArgumentNullException("Azure OpenAI configuration is missing. Please check appsettings.json.");
            }

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(deploymentName);
        }        public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($@"You are a helpful assistant designed to output JSON.
                    Generate {numberOfQuestions} multiple-choice quiz questions about {topic}.
                    Each question should have a 'QuestionText', 'Options' (an array of strings), and an 'CorrectAnswer' (the correct option string).
                    Ensure the JSON is valid and follows this structure:
                    [
                        {{
                            ""QuestionText"": ""What is the capital of France?"",
                            ""Options"": [""Berlin"", ""Madrid"", ""Paris"", ""Rome""],
                            ""CorrectAnswer"": ""Paris""
                        }},
                        {{
                            ""QuestionText"": ""Which planet is known as the Red Planet?"",
                            ""Options"": [""Earth"", ""Mars"", ""Jupiter"", ""Venus""],
                            ""CorrectAnswer"": ""Mars""
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

                // Deserialize the JSON response into a list of QuizQuestion objects
                var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return questions ?? new List<QuizQuestion>();
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger)
                Console.WriteLine($"Error generating quiz questions: {ex.Message}");
                return new List<QuizQuestion>(); // Return empty list on error
            }
        }
    }
}
