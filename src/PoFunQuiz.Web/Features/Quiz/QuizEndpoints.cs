using PoFunQuiz.Web.Models;
using System.Diagnostics;

namespace PoFunQuiz.Web.Features.Quiz;

/// <summary>
/// Minimal API endpoints for quiz operations (VSA — all quiz logic co-located).
/// </summary>
public static class QuizEndpoints
{
    private static readonly ActivitySource ActivitySource = new("PoFunQuiz");

    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        // Output-cached: 60 s, keyed on count + category — avoids redundant OpenAI calls.
        // Cache is busted server-side via the "quiz" tag if questions are invalidated.
        app.MapGet("/api/quiz/questions", async (
            int count,
            string? category,
            IOpenAIService openAIService,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("API: GetQuestions called with count={Count}, category={Category}", count, category);

            if (count <= 0)
            {
                logger.LogWarning("Invalid question count requested: {Count}", count);
                return Results.Problem(
                    detail: "Count must be a positive number.",
                    statusCode: 400,
                    title: "Invalid Request");
            }

            using var activity = ActivitySource.StartActivity("QuizGeneration");
            activity?.SetTag("quiz.question_count", count);
            activity?.SetTag("quiz.category", category ?? "General");

            var sw = Stopwatch.StartNew();
            var questions = await openAIService.GenerateQuizQuestionsAsync(
                string.IsNullOrWhiteSpace(category) ? "general knowledge" : category, count);
            sw.Stop();

            activity?.SetTag("quiz.generated_count", questions.Count);
            activity?.SetTag("quiz.duration_ms", sw.ElapsedMilliseconds);

            logger.LogInformation(
                "Generated {QuestionCount} questions in {Duration}ms",
                questions.Count,
                sw.ElapsedMilliseconds);

            return Results.Ok(questions);
        })
        .WithName("GetQuizQuestions")
        .WithOpenApi()
        .CacheOutput("QuizQuestions");
    }
}
