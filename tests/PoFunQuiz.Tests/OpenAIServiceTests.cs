using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq; // For mocking ILogger
using PoFunQuiz.Web.Services;
using System.Linq;

namespace PoFunQuiz.Tests
{

    public class OpenAIServiceTests
    {

        [Fact]
        public async Task GenerateQuizQuestionsAsync_ShouldReturnQuestions()
        {
            // Arrange
            var mockService = new Mock<IOpenAIService>();
            mockService.Setup(s => s.GenerateQuizQuestionsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<PoFunQuiz.Core.Models.QuizQuestion> {
                    new PoFunQuiz.Core.Models.QuizQuestion {
                        Question = "What is the capital of France?",
                        Options = new List<string> { "Paris", "London", "Berlin", "Rome" },
                        CorrectOptionIndex = 0
                    }
                });

            string topic = "Science";
            int numberOfQuestions = 1;

            // Act
            var questions = await mockService.Object.GenerateQuizQuestionsAsync(topic, numberOfQuestions);

            // Assert
            Assert.NotNull(questions);
            Assert.True(questions.Any(), "Expected at least one question to be returned.");
            Assert.False(string.IsNullOrEmpty(questions.First().Question));
            Assert.True(questions.First().Options.Any());
            Assert.Contains("Paris", questions.First().Options);
            Assert.Equal(0, questions.First().CorrectOptionIndex);
        }
    }
}
