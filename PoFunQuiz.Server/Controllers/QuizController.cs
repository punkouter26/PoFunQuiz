using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace PoFunQuiz.Server.Controllers
{
    // This is an API Controller (GoF Design Pattern - Front Controller/Command) that handles incoming HTTP requests
    // related to quiz operations. It uses Dependency Injection (DIP) to get the IQuestionGeneratorService.
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

        [HttpGet("generate")]
        public async Task<ActionResult<List<QuizQuestion>>> GenerateQuestions(int count)
        {
            var stopwatch = Stopwatch.StartNew();

            // Track custom event with properties
            var eventTelemetry = new EventTelemetry("QuizGeneration");
            eventTelemetry.Properties.Add("QuestionCount", count.ToString());
            eventTelemetry.Properties.Add("Category", "Random");

            if (count <= 0)
            {
                _logger.LogWarning("Invalid question count requested: {Count}", count);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCount");
                _telemetryClient.TrackEvent(eventTelemetry);
                
                return BadRequest("Count must be a positive number.");
            }

            try
            {
                var questions = await _questionGeneratorService.GenerateQuestionsAsync(count);
                
                stopwatch.Stop();
                
                // Track successful generation
                eventTelemetry.Properties.Add("Success", "true");
                eventTelemetry.Properties.Add("GeneratedCount", questions.Count.ToString());
                eventTelemetry.Metrics.Add("GenerationDurationMs", stopwatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent(eventTelemetry);
                
                // Track metric for question generation performance
                _telemetryClient.TrackMetric("QuestionGenerationTime", stopwatch.ElapsedMilliseconds);
                
                _logger.LogInformation(
                    "Generated {QuestionCount} questions in {Duration}ms",
                    questions.Count,
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

        [HttpGet("generateincategory")]
        public async Task<ActionResult<List<QuizQuestion>>> GenerateQuestionsInCategory(int count, string category)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Track custom event with properties
            var eventTelemetry = new EventTelemetry("QuizGenerationInCategory");
            eventTelemetry.Properties.Add("QuestionCount", count.ToString());
            eventTelemetry.Properties.Add("Category", category ?? "Unknown");

            _logger.LogInformation("🔍 API: GenerateQuestionsInCategory called with count={Count}, category={Category}", count, category);

            if (count <= 0)
            {
                _logger.LogWarning("🚨 API: Invalid count parameter: {Count}", count);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCount");
                _telemetryClient.TrackEvent(eventTelemetry);
                
                return BadRequest("Count must be a positive number.");
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                _logger.LogWarning("🚨 API: Invalid category parameter: {Category}", category);
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", "InvalidCategory");
                _telemetryClient.TrackEvent(eventTelemetry);
                
                return BadRequest("Category cannot be empty.");
            }

            try
            {
                var questions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(count, category);
                
                stopwatch.Stop();
                
                // Track successful generation
                eventTelemetry.Properties.Add("Success", "true");
                eventTelemetry.Properties.Add("GeneratedCount", questions?.Count.ToString() ?? "0");
                eventTelemetry.Metrics.Add("GenerationDurationMs", stopwatch.ElapsedMilliseconds);
                _telemetryClient.TrackEvent(eventTelemetry);
                
                // Track metric for category-based generation
                _telemetryClient.TrackMetric($"QuestionGeneration.{category}", stopwatch.ElapsedMilliseconds);
                
                _logger.LogInformation(
                    "🔍 API: Generated {QuestionCount} questions for category {Category} in {Duration}ms", 
                    questions?.Count ?? 0, 
                    category,
                    stopwatch.ElapsedMilliseconds);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                eventTelemetry.Properties.Add("Success", "false");
                eventTelemetry.Properties.Add("ErrorReason", ex.GetType().Name);
                _telemetryClient.TrackEvent(eventTelemetry);
                
                _logger.LogError(ex, "Error generating questions for category {Category}", category);
                throw;
            }
        }
    }
}
