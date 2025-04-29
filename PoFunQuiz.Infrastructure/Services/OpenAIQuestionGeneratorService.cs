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
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that generates quiz questions using Azure OpenAI
    /// </summary>
    public class OpenAIQuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly ILogger<OpenAIQuestionGeneratorService> _logger;
        private readonly List<QuizQuestion> _sampleQuestions;
        private readonly OpenAIClient _client;
        private readonly string _modelName;
        private readonly float _temperature;
        private readonly int _maxTokens;

        public OpenAIQuestionGeneratorService(
            ILogger<OpenAIQuestionGeneratorService> logger,
            IOptions<OpenAISettings> settings)
        {
            _logger = logger;
            _sampleQuestions = GenerateSampleQuestions();
            
            var openAISettings = settings.Value;
            
            // Initialize OpenAI client
            _client = new OpenAIClient(
                new Uri(openAISettings.Endpoint),
                new AzureKeyCredential(openAISettings.ApiKey));
            
            _modelName = openAISettings.ModelName;
            _temperature = openAISettings.Temperature;
            _maxTokens = openAISettings.MaxTokens;
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            try
            {
                _logger.LogInformation("Generating {Count} questions using OpenAI", count);

                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are a helpful quiz question generator. Generate fun and engaging questions about programming fundamentals."),
                    new ChatRequestUserMessage($"Generate {count} programming quiz questions in JSON format. Each question should have: question text as 'Question', array of 4 options as 'Options', correct option index (0-3) as 'CorrectOptionIndex', and category as 'Category'. Make questions engaging and fun.")
                };

                var chatCompletionsOptions = new ChatCompletionsOptions(_modelName, messages)
                {
                    Temperature = _temperature,
                    MaxTokens = _maxTokens
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                var jsonResponse = response.Value.Choices[0].Message.Content;

                // Parse the JSON response into QuizQuestion objects
                var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse);

                _logger.LogInformation("Successfully generated {Count} questions", questions?.Count ?? 0);
                return questions ?? _sampleQuestions.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions with OpenAI");
                // Fallback to sample questions if OpenAI fails
                return _sampleQuestions.Take(count).ToList();
            }
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            try
            {
                _logger.LogInformation("Generating {Count} questions in category {Category} using OpenAI", count, category);

                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage("You are a helpful quiz question generator. Generate fun and engaging questions about programming fundamentals."),
                    new ChatRequestUserMessage($"Generate {count} programming quiz questions about {category} in JSON format. Each question should have: question text as 'Question', array of 4 options as 'Options', correct option index (0-3) as 'CorrectOptionIndex', and category as 'Category'. Make questions engaging and fun.")
                };

                var chatCompletionsOptions = new ChatCompletionsOptions(_modelName, messages)
                {
                    Temperature = _temperature,
                    MaxTokens = _maxTokens
                };

                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                var jsonResponse = response.Value.Choices[0].Message.Content;

                // Parse the JSON response into QuizQuestion objects
                var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(jsonResponse);

                _logger.LogInformation("Successfully generated {Count} questions in category {Category}", questions?.Count ?? 0, category);
                return questions ?? _sampleQuestions.Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                                     .Take(count)
                                     .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating questions with OpenAI for category {Category}", category);
                // Fallback to sample questions in the requested category if OpenAI fails
                return _sampleQuestions.Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                                     .Take(count)
                                     .ToList();
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
                return new List<QuizQuestion>(); // Return empty list instead of null
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
                    // Add null check before filtering
                    if (questions == null)
                    {
                         _logger.LogWarning("Deserialized list was null after direct parsing attempt.");
                         return new List<QuizQuestion>();
                    }
                    if (questions.Count > 0)
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

                        // Add null check before filtering
                        if (questions == null)
                        {
                            _logger.LogWarning("Deserialized list from 'questions' property was null.");
                            return new List<QuizQuestion>();
                        }
                        
                        _logger.LogInformation("Parsed {Count} questions from 'questions' property", questions.Count);
                            
                        return FilterValidQuestions(questions);
                    }
                    
                    // If the root is an object with our expected properties, try to convert it to a list
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("text", out _))
                    {
                        // We have a single question object, not in an array
                        var singleQuestion = JsonSerializer.Deserialize<QuizQuestion>(cleanedJson, options);
                        // Add null check
                        if (singleQuestion == null)
                        {
                            _logger.LogWarning("Deserialized single question object was null.");
                            return new List<QuizQuestion>();
                        }

                        _logger.LogInformation("Parsed a single question object, converting to list");
                        return FilterValidQuestions(new List<QuizQuestion> { singleQuestion });
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
                        // Add null check
                        if (questions == null)
                        {
                            _logger.LogWarning("Deserialized list from regex extraction was null.");
                            return new List<QuizQuestion>();
                        }
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

            return new List<QuizQuestion>(); // Return empty list instead of null
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
                !string.IsNullOrWhiteSpace(q.Question) && 
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
            
            // Ensure we don't request more questions than we have available
            count = Math.Min(count, questions.Length);
            
            // Create a shuffled list of indices to ensure we get unique questions
            var random = new Random();
            var indices = Enumerable.Range(0, questions.Length).ToList();
            
            // Fisher-Yates shuffle algorithm to randomize the indices
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            
            // Take the first 'count' indices to get unique questions
            for (int i = 0; i < count; i++)
            {
                var questionIndex = indices[i];
                var question = questions[questionIndex];
                
                // Create a copy of the options
                var options = new List<string>(question.Options);
                
                // Get the correct answer
                var correctAnswer = options[question.CorrectIndex];
                
                // Randomize the order of options
                var randomizedOptions = new List<string>();
                var newCorrectIndex = 0;
                
                // Shuffle the options
                while (options.Count > 0)
                {
                    int index = random.Next(options.Count);
                    string option = options[index];
                    
                    // If this is the correct answer, note its new position
                    if (option == correctAnswer)
                    {
                        newCorrectIndex = randomizedOptions.Count;
                    }
                    
                    randomizedOptions.Add(option);
                    options.RemoveAt(index);
                }
                
                fallbackQuestions.Add(new QuizQuestion 
                { 
                    Question = question.Text,
                    Options = randomizedOptions,
                    CorrectOptionIndex = newCorrectIndex,
                    Category = question.Category,
                    Difficulty = QuestionDifficulty.Medium
                });
            }
            
            _logger.LogInformation("Generated {Count} unique fallback questions", fallbackQuestions.Count);
            return fallbackQuestions;
        }

        private List<QuizQuestion> GenerateSampleQuestions(int count = 5)
        {
            var sampleQuestions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What is the capital of France?",
                    Options = new List<string> { "Paris", "London", "Berlin", "Madrid" },
                    CorrectOptionIndex = 0,
                    Category = "Geography",
                    Difficulty = QuestionDifficulty.Easy
                },
                new QuizQuestion
                {
                    Question = "Which programming language was created by Microsoft?",
                    Options = new List<string> { "Java", "Python", "C#", "Ruby" },
                    CorrectOptionIndex = 2,
                    Category = "Technology",
                    Difficulty = QuestionDifficulty.Medium
                },
                new QuizQuestion
                {
                    Question = "What is the chemical symbol for gold?",
                    Options = new List<string> { "Ag", "Fe", "Cu", "Au" },
                    CorrectOptionIndex = 3,
                    Category = "Science",
                    Difficulty = QuestionDifficulty.Easy
                }
            };

            // Return the requested number of questions, cycling through the samples if needed
            return Enumerable.Range(0, count)
                .Select(i => sampleQuestions[i % sampleQuestions.Count])
                .ToList();
        }
    }
}
