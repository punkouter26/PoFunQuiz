using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using PoFunQuiz.Web.Models;
using System.Diagnostics;

namespace PoFunQuiz.Web.Features.Quiz;

/// <summary>
/// Minimal API endpoints for quiz operations (VSA — all quiz logic co-located).
/// </summary>
public static class QuizEndpoints
{
    public static void MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/quiz/questions", async (
            int count,
            string? category,
            IOpenAIService openAIService,
            ILogger<Program> logger,
            TelemetryClient telemetryClient) =>
        {
            var stopwatch = Stopwatch.StartNew();

            var eventTelemetry = new EventTelemetry("QuizGeneration");
            eventTelemetry.Properties.Add("QuestionCount", count.ToString());
            eventTelemetry.Properties.Add("Category", category ?? "General");

            logger.LogInformation("API: GetQuestions called with count={Count}, category={Category}", count, category);

            if (count <= 0)
            {
                logger.LogWarning("Invalid question count requested: {Count}", count);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCount");
                telemetryClient.TrackEvent(eventTelemetry);

                return Results.Problem(
                    detail: "Count must be a positive number.",
                    statusCode: 400,
                    title: "Invalid Request");
            }

            if (!string.IsNullOrWhiteSpace(category) && category.Trim().Length == 0)
            {
                logger.LogWarning("API: Invalid category parameter: {Category}", category);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCategory");
                telemetryClient.TrackEvent(eventTelemetry);

                return Results.Problem(
                    detail: "Category cannot be empty or whitespace.",
                    statusCode: 400,
                    title: "Invalid Request");
            }

            try
            {
                var questions = await openAIService.GenerateQuizQuestionsAsync(
                    string.IsNullOrWhiteSpace(category) ? "general knowledge" : category, count);

                stopwatch.Stop();

                eventTelemetry.Properties.Add("Success", "true");
                eventTelemetry.Properties.Add("GeneratedCount", questions.Count.ToString());
                eventTelemetry.Metrics.Add("GenerationDurationMs", stopwatch.ElapsedMilliseconds);
                telemetryClient.TrackEvent(eventTelemetry);

                var metricName = string.IsNullOrWhiteSpace(category)
                    ? "QuestionGenerationTime"
                    : $"QuestionGeneration.{category}";
                telemetryClient.TrackMetric(metricName, stopwatch.ElapsedMilliseconds);

                logger.LogInformation(
                    "Generated {QuestionCount} questions in {Duration}ms",
                    questions.Count,
                    stopwatch.ElapsedMilliseconds);

                return Results.Ok(questions);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", ex.GetType().Name);
                telemetryClient.TrackEvent(eventTelemetry);

                logger.LogError(ex, "Error generating questions");
                throw;
            }
        })
        .WithName("GetQuizQuestions")
        .WithOpenApi();
    }
}
