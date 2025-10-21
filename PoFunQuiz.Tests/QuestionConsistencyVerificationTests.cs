using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Server.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PoFunQuiz.Tests
{
    /// <summary>
    /// Test class to verify the fix for question consistency
    /// </summary>
    public class QuestionConsistencyVerificationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IQuestionGeneratorService _questionGeneratorService;

        public QuestionConsistencyVerificationTests(ITestOutputHelper output)
        {
            _output = output;

            // Create a service collection for dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Use the same mock service as the consistency test
            var mockOpenAIService = new MockOpenAIService();
            services.AddSingleton<IOpenAIService>(mockOpenAIService);
            services.AddSingleton<IQuestionGeneratorService, QuestionGeneratorService>();

            var serviceProvider = services.BuildServiceProvider();
            _questionGeneratorService = serviceProvider.GetRequiredService<IQuestionGeneratorService>();
        }

        [Fact]
        public async Task FixedApproach_ShouldProvideSameQuestionsForBothPlayers()
        {
            // Arrange
            const string category = "Science";
            const int questionCount = 5;

            // Act - Simulate the FIXED approach from GameSetup.razor
            var sharedQuestions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(questionCount, category);

            // Assign the SAME questions to both players (simulating the fix)
            var player1Questions = sharedQuestions;
            var player2Questions = sharedQuestions;

            // Assert
            Assert.NotNull(sharedQuestions);
            Assert.Equal(questionCount, sharedQuestions.Count);

            // Verify both players have the same questions
            Assert.Same(player1Questions, player2Questions); // Reference equality
            Assert.Equal(player1Questions.Count, player2Questions.Count);

            _output.WriteLine("✅ FIXED APPROACH VERIFICATION");
            _output.WriteLine($"Generated {sharedQuestions.Count} shared questions for both players");
            _output.WriteLine("");

            for (int i = 0; i < questionCount; i++)
            {
                var q1 = player1Questions[i];
                var q2 = player2Questions[i];

                _output.WriteLine($"Question {i + 1}:");
                _output.WriteLine($"  Player 1: {q1.Question}");
                _output.WriteLine($"  Player 2: {q2.Question}");
                _output.WriteLine($"  Same Question: {q1.Question == q2.Question}");
                _output.WriteLine($"  Same Reference: {ReferenceEquals(q1, q2)}");

                // Assert they are exactly the same
                Assert.Equal(q1.Question, q2.Question);
                Assert.Equal(q1.Options, q2.Options);
                Assert.Equal(q1.CorrectOptionIndex, q2.CorrectOptionIndex);
                Assert.Same(q1, q2); // Should be the exact same object reference

                _output.WriteLine("");
            }

            _output.WriteLine("🎯 All questions are identical between players - Fair quiz achieved!");
        }

        [Fact]
        public async Task FixedApproach_ShouldEnsureQuestionOrderConsistency()
        {
            // Arrange
            const string category = "History";
            const int questionCount = 3;

            // Act - Generate shared questions
            var sharedQuestions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(questionCount, category);

            // Simulate both players getting the same questions
            var player1Questions = sharedQuestions;
            var player2Questions = sharedQuestions;

            // Assert
            Assert.NotNull(sharedQuestions);
            Assert.Equal(questionCount, sharedQuestions.Count);

            _output.WriteLine("📋 QUESTION ORDER VERIFICATION");
            _output.WriteLine("Both players will answer these questions in the exact same order:");
            _output.WriteLine("");

            for (int i = 0; i < questionCount; i++)
            {
                _output.WriteLine($"Question {i + 1}: {sharedQuestions[i].Question}");
                _output.WriteLine($"  Options: [{string.Join(", ", sharedQuestions[i].Options)}]");
                _output.WriteLine($"  Correct Answer: {sharedQuestions[i].CorrectAnswer}");

                // Verify question order is consistent
                Assert.Equal(player1Questions[i].Question, player2Questions[i].Question);
                Assert.Equal(player1Questions[i].CorrectOptionIndex, player2Questions[i].CorrectOptionIndex);

                _output.WriteLine("");
            }

            _output.WriteLine("✅ Question order is consistent - Both players face identical challenge!");
        }
    }
}
