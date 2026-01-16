using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace PoFunQuiz.Web.Controllers
{
    /// <summary>
    /// API Controller for quiz operations using Vertical Slice Architecture pattern.
    /// Handles HTTP requests for quiz question generation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuestionGeneratorService _questionGeneratorService;
        private readonly ILogger<QuizController> _logger;
        private readonly TelemetryClient _telemetryClient;

        public QuizController(
            IQuestionGeneratorService questionGeneratorService,
            ILogger<QuizController> logger,
            TelemetryClient telemetryClient)
        {
            _questionGeneratorService = questionGeneratorService;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("questions")]
        public async Task<ActionResult<List<QuizQuestion>>> GetQuestions(
            [FromQuery] int count = 5,
            [FromQuery] string? category = null)
        {
            var stopwatch = Stopwatch.StartNew();

            // Track custom event with properties
            var eventTelemetry = new EventTelemetry("QuizGeneration");
            eventTelemetry.Properties.Add("QuestionCount", count.ToString());
            eventTelemetry.Properties.Add("Category", category ?? "General");

            _logger.LogInformation("🔍 API: GetQuestions called with count={Count}, category={Category}", count, category);

            if (count <= 0)
            {
                _logger.LogWarning("Invalid question count requested: {Count}", count);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCount");
                _telemetryClient.TrackEvent(eventTelemetry);

                return Problem(
                    detail: "Count must be a positive number.",
                    statusCode: 400,
                    title: "Invalid Request");
            }

            if (!string.IsNullOrWhiteSpace(category) && category.Trim().Length == 0)
            {
                _logger.LogWarning("🚨 API: Invalid category parameter: {Category}", category);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCategory");
                _telemetryClient.TrackEvent(eventTelemetry);

                return Problem(
                    detail: "Category cannot be empty or whitespace.",
                    statusCode: 400,
                    title: "Invalid Request");
            }

            try
            {
                List<QuizQuestion> questions;

                if (string.IsNullOrWhiteSpace(category))
                {
                    questions = await _questionGeneratorService.GenerateQuestionsAsync(count);
                }
                else
                {
                    questions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(count, category);
                }

                stopwatch.Stop();

                // Track successful generation
                eventTelemetry.Properties.Add("Success", "true");
                eventTelemetry.Properties.Add("GeneratedCount", questions?.Count.ToString() ?? "0");
                eventTelemetry.Metrics.Add("GenerationDurationMs", stopwatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent(eventTelemetry);

                // Track metric for question generation performance
                var metricName = string.IsNullOrWhiteSpace(category)
                    ? "QuestionGenerationTime"
                    : $"QuestionGeneration.{category}";
                _telemetryClient.TrackMetric(metricName, stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "Generated {QuestionCount} questions in {Duration}ms",
                    questions?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", ex.GetType().Name);
                _telemetryClient.TrackEvent(eventTelemetry);

                _logger.LogError(ex, "Error generating questions");
                throw;
            }
        }
    }
}
