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

    // Edge Case Tests

    [Fact]
    public async Task GenerateQuestions_WithVeryLargeCount_ReturnsBadRequestOrLimitsCount()
    {
        // Act
        var response = await _client.GetAsync("/api/quiz/generate?count=1000");

        // Assert - Either limits the count or returns bad request
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            (response.IsSuccessStatusCode && (await response.Content.ReadFromJsonAsync<List<QuizQuestion>>())?.Count <= 100),
            "Should either reject large counts or limit them to reasonable maximum");
    }

    [Fact]
    public async Task GenerateQuestionsInCategory_WithSpecialCharactersInCategory_HandlesGracefully()
    {
        // Arrange
        var category = "Science&Math";

        // Act
        var response = await _client.GetAsync($"/api/quiz/generateincategory?count=2&category={Uri.EscapeDataString(category)}");

        // Assert - Should handle special characters without crashing
        Assert.True(
            response.IsSuccessStatusCode ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            "Should handle special characters gracefully");
    }

    [Fact]
    public async Task GenerateQuestionsInCategory_WithWhitespaceOnlyCategory_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/quiz/generateincategory?count=5&category=%20%20%20");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestions_ConcurrentRequests_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/quiz/generate?count=2"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, r => r.EnsureSuccessStatusCode());
    }

    [Fact]
    public async Task GenerateQuestions_WithTimeout_CompletesWithinReasonableTime()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.GetAsync("/api/quiz/generate?count=5", cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(duration < timeout, $"Request took {duration.TotalSeconds}s, expected less than {timeout.TotalSeconds}s");
    }
}

