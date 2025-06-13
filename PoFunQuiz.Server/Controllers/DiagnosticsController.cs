using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Server.Services;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Server.Controllers
{
    // This is a diagnostics controller (GoF Design Pattern - Front Controller) that provides health check endpoints
    // for testing connections to external services like Azure OpenAI.
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(IOpenAIService openAIService, ILogger<DiagnosticsController> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet("openai")]
        public async Task<IActionResult> TestOpenAIConnection()
        {
            try
            {
                _logger.LogInformation("Testing OpenAI connection...");
                
                // Test with a simple question
                var questions = await _openAIService.GenerateQuizQuestionsAsync("simple test", 1);
                
                if (questions != null && questions.Any())
                {
                    _logger.LogInformation("OpenAI connection test successful - received {QuestionCount} questions", questions.Count);
                    return Ok(new 
                    { 
                        status = "success", 
                        message = $"OpenAI connection successful. Generated {questions.Count} question(s).",
                        timestamp = DateTime.UtcNow,
                        questionCount = questions.Count
                    });
                }
                else
                {
                    _logger.LogWarning("OpenAI connection test returned empty result");
                    return Ok(new 
                    { 
                        status = "warning", 
                        message = "OpenAI connection succeeded but returned no questions. Check API configuration.",
                        timestamp = DateTime.UtcNow,
                        questionCount = 0
                    });
                }
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "OpenAI configuration missing");
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "OpenAI configuration is missing. Please check appsettings.json.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI network error");
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "Network error connecting to OpenAI. Check your internet connection and API endpoint.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI connection test failed");
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "OpenAI connection test failed. Check your API key and configuration.",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }

        [HttpGet("api")]
        public async Task<IActionResult> TestAPIHealth()
        {
            try
            {
                // Test internal API health
                var result = new
                {
                    status = "success",
                    message = "API is responding correctly",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    version = "1.0.0"
                };
                
                _logger.LogInformation("API health check successful");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API health check failed");
                return BadRequest(new 
                { 
                    status = "error", 
                    message = "API health check failed",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}
