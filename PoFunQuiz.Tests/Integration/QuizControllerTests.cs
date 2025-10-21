using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Tests.Integration;

public class QuizControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public QuizControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateQuestions_WithValidCount_ReturnsQuestions()
    {
        // Arrange
        var count = 5;

        // Act
        var response = await _client.GetAsync($"/api/quiz/generate?count={count}");

        // Assert
        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.Equal(count, questions.Count);
        
        // Verify question structure
        foreach (var question in questions)
        {
            Assert.NotEmpty(question.Question);
            Assert.NotEmpty(question.CorrectAnswer);
            Assert.NotEmpty(question.Options);
            Assert.True(question.Options.Count >= 2);
            Assert.Contains(question.CorrectAnswer, question.Options);
        }
    }

    [Fact]
    public async Task GenerateQuestions_WithZeroCount_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/quiz/generate?count=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestions_WithNegativeCount_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/quiz/generate?count=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestionsInCategory_WithValidParameters_ReturnsQuestions()
    {
        // Arrange
        var count = 3;
        var category = "Science";

        // Act
        var response = await _client.GetAsync($"/api/quiz/generateincategory?count={count}&category={category}");

        // Assert
        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.Equal(count, questions.Count);
        
        // Verify all questions are in the requested category
        foreach (var question in questions)
        {
            Assert.Equal(category, question.Category);
        }
    }

    [Fact]
    public async Task GenerateQuestionsInCategory_WithEmptyCategory_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/quiz/generateincategory?count=5&category=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Science")]
    [InlineData("History")]
    [InlineData("Geography")]
    [InlineData("Sports")]
    public async Task GenerateQuestionsInCategory_WithDifferentCategories_ReturnsAppropriateQuestions(string category)
    {
        // Act
        var response = await _client.GetAsync($"/api/quiz/generateincategory?count=2&category={category}");

        // Assert
        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.All(questions, q => Assert.Equal(category, q.Category));
    }
}
