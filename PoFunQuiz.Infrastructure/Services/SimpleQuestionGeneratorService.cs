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
    /// This service is disabled. All sample question logic has been removed. Use OpenAI for question generation.
    /// </summary>
    public class OpenAIQuestionGeneratorService : IQuestionGeneratorService
    {
        public Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            throw new NotImplementedException("Sample question generator is disabled. Use OpenAI for question generation.");
        }

        public Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            throw new NotImplementedException("Sample question generator is disabled. Use OpenAI for question generation.");
        }
    }
}
