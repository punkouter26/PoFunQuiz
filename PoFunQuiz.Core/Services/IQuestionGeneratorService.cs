using System.Collections.Generic;
using System.Threading.Tasks;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Core.Services
{
    /// <summary>
    /// Service interface for generating quiz questions
    /// </summary>
    public interface IQuestionGeneratorService
    {
        /// <summary>
        /// Generates a specified number of quiz questions asynchronously
        /// </summary>
        /// <param name="count">Number of questions to generate</param>
        /// <returns>A list of generated quiz questions</returns>
        Task<List<QuizQuestion>> GenerateQuestionsAsync(int count);
        
        /// <summary>
        /// Generates a specified number of questions in a specific category
        /// </summary>
        /// <param name="count">Number of questions to generate</param>
        /// <param name="category">Category of questions (e.g., "Science", "History")</param>
        /// <returns>A list of generated quiz questions in the specified category</returns>
        Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category);
    }
}