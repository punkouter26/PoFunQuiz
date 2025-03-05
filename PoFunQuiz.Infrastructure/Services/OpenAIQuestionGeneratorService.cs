using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Core.Configuration;

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that generates quiz questions using Azure OpenAI
    /// </summary>
    public class OpenAIQuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        private readonly ILogger<OpenAIQuestionGeneratorService> _logger;

        public OpenAIQuestionGeneratorService(
            IOptions<OpenAISettings> settings,
            ILogger<OpenAIQuestionGeneratorService> logger)
        {
            var openAISettings = settings.Value;
            
            _client = new OpenAIClient(
                new Uri(openAISettings.Endpoint),
                new AzureKeyCredential(openAISettings.Key));
            
            _deploymentName = openAISettings.DeploymentName;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            try
            {
                _logger.LogInformation("Starting to generate {Count} questions", count);
                
                // IMPORTANT: Temporarily return fallback questions directly for all requests
                // to avoid OpenAI issues during testing/development
                return GenerateFallbackQuestions(count);

                // The following code is commented out until OpenAI issues are resolved
                /*
                // Generate random categories for more variety
                var categories = new List<string> { "Science", "History", "Geography", "Literature", "Movies", "Technology" };
                var random = new Random();

                var questions = new List<QuizQuestion>();
                
                // Generate questions in batches for better performance
                const int batchSize = 5;
                for (int i = 0; i < count; i += batchSize)
                {
                    int currentBatchSize = Math.Min(batchSize, count - i);
                    var batchCategory = categories[random.Next(categories.Count)];
                    
                    var batchQuestions = await GenerateQuestionsInCategoryAsync(currentBatchSize, batchCategory);
                    if (batchQuestions != null && batchQuestions.Count > 0)
                    {
                        questions.AddRange(batchQuestions);
                    }
                    else
                    {
                        // If we get no questions, try with a different category
                        batchCategory = categories[random.Next(categories.Count)];
                        batchQuestions = await GenerateQuestionsInCategoryAsync(currentBatchSize, batchCategory);
                        if (batchQuestions != null && batchQuestions.Count > 0)
                        {
                            questions.AddRange(batchQuestions);
                        }
                    }
                }

                // If we couldn't generate enough questions, create some fallback questions
                if (questions.Count < count)
                {
                    _logger.LogWarning("Could not generate enough questions using OpenAI. Using fallback questions.");
                    questions.AddRange(GenerateFallbackQuestions(count - questions.Count));
                }

                return questions;
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz questions");
                // Return fallback questions instead of throwing
                return GenerateFallbackQuestions(count);
            }
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            try
            {
                var systemMessage = "You are a quiz generator assistant that creates challenging but fair multiple-choice questions.";
                var userMessage = $@"Generate {count} challenging quiz questions about {category}. Each question should have exactly 4 multiple-choice options with only one correct answer.

Return the results in valid JSON format as an array of objects with this structure:
[{{
  ""text"": ""Question text goes here?"",
  ""options"": [
    ""Option A"",
    ""Option B"",
    ""Option C"",
    ""Option D""
  ],
  ""correctOptionIndex"": 0,
  ""category"": ""{category}"",
  ""difficulty"": ""Medium""
}}]

Note: correctOptionIndex is zero-based (0 means the first option is correct).
Make the questions challenging but answerable without specialized knowledge.
Create plausible but clearly incorrect options.
IMPORTANT: Return ONLY a valid JSON array with no additional text.";

                var chatCompletions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage(userMessage)
                    },
                    MaxTokens = 2500,
                    Temperature = 0.7f,
                    NucleusSamplingFactor = 0.95f,
                    FrequencyPenalty = 0.8f,
                    PresencePenalty = 0.8f,
                    ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletions);
                var jsonResponse = response.Value.Choices[0].Message.Content;
                
                // Log the actual JSON response for debugging
                _logger.LogInformation("OpenAI JSON response: {Response}", jsonResponse);

                // Handle the JSON parsing with multiple fallback mechanisms
                return ParseOpenAIResponse(jsonResponse, category) ?? new List<QuizQuestion>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz questions in category {Category}", category);
                return new List<QuizQuestion>();
            }
        }

        /// <summary>
        /// Parses the OpenAI response with multiple fallback strategies
        /// </summary>
        private List<QuizQuestion> ParseOpenAIResponse(string jsonResponse, string category)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogError("OpenAI returned empty or null response");
                return null;
            }

            try
            {
                // Try direct deserialization first
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                // Clean up any non-JSON content that might be present
                var cleanedJson = CleanJsonResponse(jsonResponse);
                _logger.LogInformation("Cleaned JSON: {CleanedJson}", cleanedJson);

                // Try to deserialize as a list first
                try
                {
                    var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(cleanedJson, options);
                    if (questions != null && questions.Count > 0)
                    {
                        _logger.LogInformation("Successfully parsed {Count} questions directly", questions.Count);
                        return FilterValidQuestions(questions);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to parse as list: {Error}", ex.Message);
                }

                // If that fails, try to parse as an object with a "questions" property
                try
                {
                    using var doc = JsonDocument.Parse(cleanedJson);
                    
                    // Check for "questions" property
                    if (doc.RootElement.TryGetProperty("questions", out var questionsElement) &&
                        questionsElement.ValueKind == JsonValueKind.Array)
                    {
                        var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(
                            questionsElement.GetRawText(), options);
                        
                        _logger.LogInformation("Parsed {Count} questions from 'questions' property", 
                            questions?.Count ?? 0);
                            
                        return FilterValidQuestions(questions);
                    }
                    
                    // If the root is an object with our expected properties, try to convert it to a list
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("text", out _))
                    {
                        // We have a single question object, not in an array
                        var singleQuestion = JsonSerializer.Deserialize<QuizQuestion>(cleanedJson, options);
                        if (singleQuestion != null)
                        {
                            _logger.LogInformation("Parsed a single question object, converting to list");
                            return FilterValidQuestions(new List<QuizQuestion> { singleQuestion });
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to parse JSON document: {Error}", ex.Message);
                }

                // If still not successful, try to extract valid JSON from the response using regex
                try
                {
                    var arrayMatch = Regex.Match(jsonResponse, @"\[[\s\S]*\]");
                    if (arrayMatch.Success)
                    {
                        _logger.LogInformation("Found JSON array using regex");
                        var extractedArray = arrayMatch.Value;
                        var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(extractedArray, options);
                        return FilterValidQuestions(questions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to extract JSON array using regex: {Error}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All JSON parsing attempts failed");
            }

            return null;
        }

        /// <summary>
        /// Clean the JSON response to remove any non-JSON content
        /// </summary>
        private string CleanJsonResponse(string jsonResponse)
        {
            // If it looks like the response starts with markdown code blocks, extract just the JSON
            if (jsonResponse.Contains("```json"))
            {
                var match = Regex.Match(jsonResponse, @"```json\s*([\s\S]*?)\s*```");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            // If it's just wrapped in backticks
            if (jsonResponse.Contains("```"))
            {
                var match = Regex.Match(jsonResponse, @"```\s*([\s\S]*?)\s*```");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            // If there's explanatory text before or after the JSON
            var openBracket = jsonResponse.IndexOf('[');
            var closeBracket = jsonResponse.LastIndexOf(']');
            if (openBracket >= 0 && closeBracket > openBracket)
            {
                return jsonResponse.Substring(openBracket, closeBracket - openBracket + 1);
            }

            // Just return the original if we can't find a clear pattern
            return jsonResponse;
        }

        /// <summary>
        /// Filter valid questions from the parsed list
        /// </summary>
        private List<QuizQuestion> FilterValidQuestions(List<QuizQuestion> questions)
        {
            if (questions == null || questions.Count == 0)
            {
                return new List<QuizQuestion>();
            }

            return questions.FindAll(q => 
                !string.IsNullOrWhiteSpace(q.Text) && 
                q.Options?.Count == 4 &&
                q.CorrectOptionIndex >= 0 && 
                q.CorrectOptionIndex < 4);
        }

        /// <summary>
        /// Generates fallback questions if the OpenAI service fails
        /// </summary>
        private List<QuizQuestion> GenerateFallbackQuestions(int count)
        {
            _logger.LogInformation("Generating {Count} fallback questions", count);
            var fallbackQuestions = new List<QuizQuestion>();
            
            // Add some simple predefined questions
            var questions = new[]
            {
                new { 
                    Text = "What is the capital of France?", 
                    Options = new List<string> { "Paris", "London", "Berlin", "Madrid" }, 
                    CorrectIndex = 0,
                    Category = "Geography"
                },
                new { 
                    Text = "Who painted the Mona Lisa?", 
                    Options = new List<string> { "Leonardo da Vinci", "Vincent van Gogh", "Pablo Picasso", "Michelangelo" }, 
                    CorrectIndex = 0,
                    Category = "Art"
                },
                new { 
                    Text = "What planet is known as the Red Planet?", 
                    Options = new List<string> { "Mars", "Venus", "Jupiter", "Saturn" }, 
                    CorrectIndex = 0,
                    Category = "Science"
                },
                new { 
                    Text = "Which element has the chemical symbol 'O'?", 
                    Options = new List<string> { "Oxygen", "Gold", "Iron", "Carbon" }, 
                    CorrectIndex = 0,
                    Category = "Science"
                },
                new { 
                    Text = "Which country is known as the Land of the Rising Sun?", 
                    Options = new List<string> { "Japan", "China", "Thailand", "South Korea" }, 
                    CorrectIndex = 0,
                    Category = "Geography"
                },
                new { 
                    Text = "Who wrote the play 'Romeo and Juliet'?", 
                    Options = new List<string> { "William Shakespeare", "Charles Dickens", "Jane Austen", "Mark Twain" }, 
                    CorrectIndex = 0,
                    Category = "Literature"
                },
                new { 
                    Text = "What is the main ingredient in guacamole?", 
                    Options = new List<string> { "Avocado", "Tomato", "Lime", "Onion" }, 
                    CorrectIndex = 0,
                    Category = "Food"
                },
                new { 
                    Text = "Which animal is known as the King of the Jungle?", 
                    Options = new List<string> { "Lion", "Tiger", "Elephant", "Gorilla" }, 
                    CorrectIndex = 0,
                    Category = "Animals"
                },
                new { 
                    Text = "What is the largest planet in our solar system?", 
                    Options = new List<string> { "Jupiter", "Saturn", "Earth", "Neptune" }, 
                    CorrectIndex = 0,
                    Category = "Science"
                },
                new { 
                    Text = "What year did the Titanic sink?", 
                    Options = new List<string> { "1912", "1905", "1920", "1931" }, 
                    CorrectIndex = 0,
                    Category = "History"
                },
                new { 
                    Text = "Which programming language is known for its use in web development?", 
                    Options = new List<string> { "JavaScript", "C++", "Swift", "COBOL" }, 
                    CorrectIndex = 0,
                    Category = "Technology"
                },
                new { 
                    Text = "Who directed the movie 'Jurassic Park'?", 
                    Options = new List<string> { "Steven Spielberg", "James Cameron", "Christopher Nolan", "Quentin Tarantino" }, 
                    CorrectIndex = 0,
                    Category = "Movies"
                },
                new { 
                    Text = "What is the chemical symbol for gold?", 
                    Options = new List<string> { "Au", "Ag", "Fe", "Cu" }, 
                    CorrectIndex = 0,
                    Category = "Science"
                },
                new { 
                    Text = "Which U.S. state is known as the Sunshine State?", 
                    Options = new List<string> { "Florida", "California", "Texas", "Hawaii" }, 
                    CorrectIndex = 0,
                    Category = "Geography"
                },
                new { 
                    Text = "Who wrote 'The Great Gatsby'?", 
                    Options = new List<string> { "F. Scott Fitzgerald", "Ernest Hemingway", "Mark Twain", "J.D. Salinger" }, 
                    CorrectIndex = 0,
                    Category = "Literature"
                },
                new { 
                    Text = "What is the capital of Japan?", 
                    Options = new List<string> { "Tokyo", "Kyoto", "Osaka", "Hiroshima" }, 
                    CorrectIndex = 0,
                    Category = "Geography"
                },
                new { 
                    Text = "Which of these is not a primary color?", 
                    Options = new List<string> { "Green", "Blue", "Red", "Yellow" }, 
                    CorrectIndex = 0,
                    Category = "Art"
                },
                new { 
                    Text = "Who is known as the father of modern physics?", 
                    Options = new List<string> { "Albert Einstein", "Isaac Newton", "Galileo Galilei", "Stephen Hawking" }, 
                    CorrectIndex = 0,
                    Category = "Science"
                },
                new { 
                    Text = "Which ocean is the largest?", 
                    Options = new List<string> { "Pacific", "Atlantic", "Indian", "Arctic" }, 
                    CorrectIndex = 0,
                    Category = "Geography"
                },
                new { 
                    Text = "What was the first feature-length animated movie ever released?", 
                    Options = new List<string> { "Snow White and the Seven Dwarfs", "Pinocchio", "Fantasia", "Bambi" }, 
                    CorrectIndex = 0,
                    Category = "Movies"
                }
            };
            
            // Use random ones from our predefined list to fill up to the count
            var random = new Random();
            for (int i = 0; i < count; i++)
            {
                var question = questions[random.Next(questions.Length)];
                fallbackQuestions.Add(new QuizQuestion 
                { 
                    Text = question.Text,
                    Options = new List<string>(question.Options),
                    CorrectOptionIndex = question.CorrectIndex,
                    Category = question.Category,
                    Difficulty = "Medium"
                });
            }
            
            return fallbackQuestions;
        }
    }
}