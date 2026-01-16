using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoFunQuiz.Web.Services
{
    // This class implements the IQuestionGeneratorService interface for the client-side.
    // It acts as a Proxy (GoF Design Pattern) to the server-side QuestionGeneratorService.
    public class ClientQuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly HttpClient _httpClient;

        public ClientQuestionGeneratorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsAsync(int count)
        {
            // Call the server-side API to generate questions
            return await _httpClient.GetFromJsonAsync<List<QuizQuestion>>($"api/quiz/questions?count={count}") ?? new List<QuizQuestion>();
        }

        public async Task<List<QuizQuestion>> GenerateQuestionsInCategoryAsync(int count, string category)
        {
            // Call the server-side API to generate questions in a specific category
            return await _httpClient.GetFromJsonAsync<List<QuizQuestion>>($"api/quiz/questions?count={count}&category={category}") ?? new List<QuizQuestion>();
        }
    }
}
