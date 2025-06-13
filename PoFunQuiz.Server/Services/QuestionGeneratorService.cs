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
            // Use a default topic if no category is specified
            return await _openAIService.GenerateQuizQuestionsAsync("general knowledge", count);
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            return await _openAIService.GenerateQuizQuestionsAsync(category, count);
        }
    }
}
