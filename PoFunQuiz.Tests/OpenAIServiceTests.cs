using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq; // For mocking ILogger
using PoFunQuiz.Server.Services;
using System.Linq;

namespace PoFunQuiz.Tests
{
    public class OpenAIServiceTests
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIServiceTests()
        {
            // Build configuration from appsettings.Development.json
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
                .Build();

            // Mock ILogger for OpenAIService
            _logger = Mock.Of<ILogger<OpenAIService>>();
        }

        [Fact]
        public async Task GenerateQuizQuestionsAsync_ShouldReturnQuestions()
        {
            // Arrange
            var openAIService = new OpenAIService(_configuration, _logger);
            string topic = "Science";
            int numberOfQuestions = 1;

            // Act
            var questions = await openAIService.GenerateQuizQuestionsAsync(topic, numberOfQuestions);

            // Assert
            Assert.NotNull(questions);
            Assert.True(questions.Any(), "Expected at least one question to be returned.");
            Assert.Equal(numberOfQuestions, questions.Count); // Assert the exact number of questions
            Assert.False(string.IsNullOrEmpty(questions.First().Question));
            Assert.True(questions.First().Options.Any());
            Assert.True(questions.First().CorrectOptionIndex >= 0 && questions.First().CorrectOptionIndex < questions.First().Options.Count);
        }
    }
}
