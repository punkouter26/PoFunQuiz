using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Web.Services;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that mocks external services (OpenAI) for integration testing.
/// This follows the PoTestAll requirement: "Prohibit calls to live LLM APIs during testing"
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing IOpenAIService registration
            var openAIDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOpenAIService));
            if (openAIDescriptor != null)
            {
                services.Remove(openAIDescriptor);
            }

            // Remove existing IQuestionGeneratorService registration
            var qgDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IQuestionGeneratorService));
            if (qgDescriptor != null)
            {
                services.Remove(qgDescriptor);
            }

            // Add mock OpenAI service
            services.AddSingleton<IOpenAIService, MockOpenAIService>();
            
            // Add question generator that uses the mock
            services.AddSingleton<IQuestionGeneratorService, QuestionGeneratorService>();
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Mock OpenAI service for testing - returns deterministic results without calling live APIs
/// </summary>
public class MockOpenAIService : IOpenAIService
{
    public Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
    {
        var questions = new List<QuizQuestion>();

        for (int i = 0; i < numberOfQuestions; i++)
        {
            var questionId = i + 1;
            var question = new QuizQuestion
            {
                Question = $"Mock {topic ?? "General"} question #{questionId}: What is the capital of mock country {questionId}?",
                Options = new List<string>
                {
                    $"Mock City A",
                    $"Mock City B",
                    $"Mock City C",
                    $"Mock City D"
                },
                CorrectOptionIndex = 0, // "Mock City A" is the correct answer
                Difficulty = QuestionDifficulty.Easy,
                Category = topic ?? "General"
            };
            questions.Add(question);
        }

        return Task.FromResult(questions);
    }
}
