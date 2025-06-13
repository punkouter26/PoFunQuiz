using Microsoft.AspNetCore.Mvc;
using PoFunQuiz.Core.Models;
using PoFunQuiz.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoFunQuiz.Server.Controllers
{
    // This is an API Controller (GoF Design Pattern - Front Controller/Command) that handles incoming HTTP requests
    // related to quiz operations. It uses Dependency Injection (DIP) to get the IQuestionGeneratorService.
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuestionGeneratorService _questionGeneratorService;

        public QuizController(IQuestionGeneratorService questionGeneratorService)
        {
            _questionGeneratorService = questionGeneratorService;
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
            if (count <= 0)
            {
                return BadRequest("Count must be a positive number.");
            }
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Category cannot be empty.");
            }
            var questions = await _questionGeneratorService.GenerateQuestionsInCategoryAsync(count, category);
            return Ok(questions);
        }
    }
}
