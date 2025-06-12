using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Core.Configuration;
using System.Linq;

namespace PoFunQuiz.Infrastructure.Services
{
    /// <summary>
    /// Service that generates quiz questions using sample data (OpenAI temporarily disabled)
    /// </summary>
    public class OpenAIQuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly ILogger<OpenAIQuestionGeneratorService> _logger;
        private readonly List<QuizQuestion> _sampleQuestions;

        public OpenAIQuestionGeneratorService(
            ILogger<OpenAIQuestionGeneratorService> logger,
            IOptions<OpenAISettings> settings)
        {
            _logger = logger;
            _sampleQuestions = GenerateSampleQuestions();
            _logger.LogInformation("OpenAIQuestionGeneratorService initialized with sample questions");
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            _logger.LogInformation("Generating {Count} sample questions", count);
            await Task.Delay(100); // Simulate async operation
            return _sampleQuestions.Take(count).ToList();
        }

        /// <inheritdoc />
        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            _logger.LogInformation("Generating {Count} sample questions for category {Category}", count, category);
            await Task.Delay(100); // Simulate async operation
            return _sampleQuestions
                .Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Take(count)
                .ToList();
        }

        private List<QuizQuestion> GenerateSampleQuestions()
        {
            return new List<QuizQuestion>
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
                },
                new QuizQuestion
                {
                    Question = "Who painted the Mona Lisa?",
                    Options = new List<string> { "Leonardo da Vinci", "Vincent van Gogh", "Pablo Picasso", "Michelangelo" },
                    CorrectOptionIndex = 0,
                    Category = "Art",
                    Difficulty = QuestionDifficulty.Medium
                },
                new QuizQuestion
                {
                    Question = "What is the largest planet in our solar system?",
                    Options = new List<string> { "Jupiter", "Saturn", "Earth", "Neptune" },
                    CorrectOptionIndex = 0,
                    Category = "Science",
                    Difficulty = QuestionDifficulty.Easy
                },
                new QuizQuestion
                {
                    Question = "Which HTTP status code indicates 'Not Found'?",
                    Options = new List<string> { "200", "404", "500", "301" },
                    CorrectOptionIndex = 1,
                    Category = "Technology",
                    Difficulty = QuestionDifficulty.Medium
                },
                new QuizQuestion
                {
                    Question = "What does CSS stand for?",
                    Options = new List<string> { "Computer Style Sheets", "Cascading Style Sheets", "Creative Style Sheets", "Colorful Style Sheets" },
                    CorrectOptionIndex = 1,
                    Category = "Technology",
                    Difficulty = QuestionDifficulty.Easy
                },
                new QuizQuestion
                {
                    Question = "Which ocean is the largest?",
                    Options = new List<string> { "Pacific", "Atlantic", "Indian", "Arctic" },
                    CorrectOptionIndex = 0,
                    Category = "Geography",
                    Difficulty = QuestionDifficulty.Easy
                }
            };
        }
    }
}
