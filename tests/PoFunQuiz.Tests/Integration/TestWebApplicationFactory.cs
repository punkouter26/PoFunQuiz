using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PoFunQuiz.Web.Models;
using PoFunQuiz.Web.Features.Quiz;

namespace PoFunQuiz.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that mocks external services (OpenAI) for integration testing.
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

            // Add mock OpenAI service
            services.AddSingleton<IOpenAIService, MockOpenAIService>();
        });

        // Use Development environment to skip HTTPS redirect in tests
        builder.UseEnvironment("Development");
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
                CorrectOptionIndex = 0,
                Difficulty = QuestionDifficulty.Easy,
                Category = topic ?? "General"
            };
            questions.Add(question);
        }

        return Task.FromResult(questions);
    }
}
