using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoFunQuiz.Server.Services
{
    // This class acts as an Adapter (GoF Design Pattern) to adapt the IOpenAIService to the IQuestionGeneratorService interface.
    // It also adheres to the Dependency Inversion Principle (DIP) by depending on the IOpenAIService abstraction.
    public class QuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly IOpenAIService _openAIService;

        public QuestionGeneratorService(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            // Try to generate via OpenAI, but fall back to local sample questions on error or empty result
            try
            {
                var result = await _openAIService.GenerateQuizQuestionsAsync("general knowledge", count);
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }
            catch
            {
                // Swallow exceptions here to allow fallback to sample questions
            }

            return GetSampleQuestions(count);
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            try
            {
                var result = await _openAIService.GenerateQuizQuestionsAsync(category, count);
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }
            catch
            {
                // fall through to sample fallback
            }

            return GetSampleQuestionsForCategory(count, category);
        }

        // Local fallback sample questions to keep the app usable when OpenAI is not configured or unreachable
        private static List<QuizQuestion> GetSampleQuestions(int count)
        {
            var samples = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What is the capital of France?",
                    Options = new List<string> { "Paris", "London", "Berlin", "Madrid" },
                    CorrectOptionIndex = 0,
                    Category = "Geography"
                },
                new QuizQuestion
                {
                    Question = "Which planet is known as the Red Planet?",
                    Options = new List<string> { "Earth", "Mars", "Jupiter", "Venus" },
                    CorrectOptionIndex = 1,
                    Category = "Science"
                },
                new QuizQuestion
                {
                    Question = "Which language is primarily used for web styling?",
                    Options = new List<string> { "HTML", "C#", "CSS", "SQL" },
                    CorrectOptionIndex = 2,
                    Category = "Technology"
                },
                new QuizQuestion
                {
                    Question = "Who painted the Mona Lisa?",
                    Options = new List<string> { "Leonardo da Vinci", "Vincent van Gogh", "Pablo Picasso", "Rembrandt" },
                    CorrectOptionIndex = 0,
                    Category = "Art"
                }
            };

            return samples.Take(count).ToList();
        }

        private static List<QuizQuestion> GetSampleQuestionsForCategory(int count, string category)
        {
            var all = GetSampleQuestions(int.MaxValue);
            var filtered = all.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
            if (filtered.Count == 0)
            {
                // If no questions match the category, just return top samples
                return GetSampleQuestions(count);
            }

            return filtered.Take(count).ToList();
        }
    }
}
