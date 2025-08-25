using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

        public QuizController(IQuestionGeneratorService questionGeneratorService, ILogger<QuizController> logger)
        {
            _questionGeneratorService = questionGeneratorService;
            _logger = logger;
        }

        [HttpGet("generate")]
        public async Task<ActionResult<List<QuizQuestion>>> GenerateQuestions(int count)
        {
            if (count <= 0)
            {
                return BadRequest("Count must be a positive number.");
            }
            var questions = await _questionGeneratorService.GenerateQuestionsAsync(count);
            return Ok(questions);
        }

        [HttpGet("generateincategory")]
        public async Task<ActionResult<List<QuizQuestion>>> GenerateQuestionsInCategory(int count, string category)
        {
            _logger.LogInformation("🔍 API: GenerateQuestionsInCategory called with count={Count}, category={Category}", count, category);

            if (count <= 0)
            {
                _logger.LogWarning("🚨 API: Invalid count parameter: {Count}", count);
                return BadRequest("Count must be a positive number.");
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                _logger.LogWarning("🚨 API: Invalid category parameter: {Category}", category);
                return BadRequest("Category cannot be empty.");
            }

            var questions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(count, category);
            _logger.LogInformation("🔍 API: Generated {QuestionCount} questions for category {Category}", questions?.Count ?? 0, category);

            return Ok(questions);
        }
    }
}
