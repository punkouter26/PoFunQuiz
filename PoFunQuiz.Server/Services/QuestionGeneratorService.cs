using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PoFunQuiz.Server.Services
{
    // This class acts as an Adapter (GoF Design Pattern) to adapt the IOpenAIService to the IQuestionGeneratorService interface.
    // It also adheres to the Dependency Inversion Principle (DIP) by depending on the IOpenAIService abstraction.
    public class QuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<QuestionGeneratorService> _logger;

        public QuestionGeneratorService(IOpenAIService openAIService, ILogger<QuestionGeneratorService> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
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
                // Log exception if needed
            }
            // No fallback: return empty list if OpenAI fails
            return new List<QuizQuestion>();
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            _logger.LogInformation("🔍 SERVICE: GenerateQuestionsInCategoryAsync called with count={Count}, category={Category}", count, category);
            
            try
            {
                var result = await _openAIService.GenerateQuizQuestionsAsync(category, count);
                _logger.LogInformation("🔍 SERVICE: OpenAI returned {QuestionCount} questions", result?.Count ?? 0);
                
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 SERVICE: Exception in GenerateQuestionsInCategoryAsync");
            }
            
            _logger.LogWarning("🚨 SERVICE: Returning empty list for category {Category}", category);
            // No fallback: return empty list if OpenAI fails
            return new List<QuizQuestion>();
        }


    }
}
