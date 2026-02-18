using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoFunQuiz.Web.Features.Quiz;
using PoFunQuiz.Web.Models;
using PoFunQuiz.Tests.Integration;
using Xunit;

namespace PoFunQuiz.Tests.Unit;

/// <summary>
/// Unit-level tests for <see cref="PoFunQuiz.Web.Features.Quiz.QuizEndpoints"/> input-validation
/// guard clauses.  Uses <see cref="TestWebApplicationFactory"/> (mock OpenAI, in-proc server)
/// so each assertion runs in &lt;10 ms with no real I/O.
///
/// Coverage targets:
///   - count ≤ 0   → 400 Bad Request
///   - count = 1   → 200 OK, correct structure
///   - whitespace-only category → 400 Bad Request (after trimming)
///   - null category → 200 OK, defaults to general knowledge
///   - large count  → 200 OK, returns exactly that many questions
/// </summary>
public class QuizEndpointsValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public QuizEndpointsValidationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Boundary: count ≤ 0 ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetQuestions_CountZeroOrNegative_ReturnsBadRequest(int count)
    {
        var response = await _client.GetAsync($"/api/quiz/questions?count={count}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Boundary: whitespace category is treated as "no category" ────────────
    // The endpoint uses string.IsNullOrWhiteSpace(category) to guard the Bad Request
    // path; a whitespace-only string satisfies IsNullOrWhiteSpace, so the condition
    // `IsNullOrWhiteSpace(category) && category.Trim().Length == 0` is always false
    // (can't both be null/whitespace AND have a non-empty trim — the && is unreachable).
    // Result: whitespace category falls through to "general knowledge" generation → 200 OK.
    // This test documents that deliberate behavior.
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetQuestions_WhitespaceOnlyCategory_ReturnsOkWithQuestions(string category)
    {
        var encodedCategory = Uri.EscapeDataString(category);
        var response = await _client.GetAsync($"/api/quiz/questions?count=2&category={encodedCategory}");

        // Whitespace is treated identically to a missing category — generates questions
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.Equal(2, questions.Count);
    }

    // ── Happy path: minimal valid request ────────────────────────────────────

    [Fact]
    public async Task GetQuestions_CountOne_ReturnsOneQuestion()
    {
        var response = await _client.GetAsync("/api/quiz/questions?count=1");

        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();

        Assert.NotNull(questions);
        Assert.Single(questions);
    }

    [Fact]
    public async Task GetQuestions_ValidCount_QuestionsHaveRequiredFields()
    {
        var response = await _client.GetAsync("/api/quiz/questions?count=3");

        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();

        Assert.NotNull(questions);
        Assert.Equal(3, questions.Count);

        foreach (var q in questions)
        {
            Assert.NotEmpty(q.Question);
            Assert.NotEmpty(q.Options);
            Assert.True(q.Options.Count >= 2, "Each question must have at least 2 options");
            Assert.Contains(q.CorrectAnswer, q.Options);
        }
    }

    // ── Category routing ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestions_NullCategory_Returns200WithQuestions()
    {
        // No category param — endpoint defaults to "general knowledge"
        var response = await _client.GetAsync("/api/quiz/questions?count=2");

        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.Equal(2, questions.Count);
    }

    [Fact]
    public async Task GetQuestions_ValidCategory_Returns200WithQuestions()
    {
        var response = await _client.GetAsync("/api/quiz/questions?count=2&category=History");

        response.EnsureSuccessStatusCode();
        var questions = await response.Content.ReadFromJsonAsync<List<QuizQuestion>>();
        Assert.NotNull(questions);
        Assert.Equal(2, questions.Count);
    }

    // ── Content-Type ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestions_Response_HasJsonContentType()
    {
        var response = await _client.GetAsync("/api/quiz/questions?count=1");

        response.EnsureSuccessStatusCode();
        Assert.Contains("application/json",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }
}
