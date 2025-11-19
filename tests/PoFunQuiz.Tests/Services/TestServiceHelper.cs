using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Server.Services;
using System;

namespace PoFunQuiz.Tests
{
    /// <summary>
    /// Helper class for building service providers in test classes
    /// </summary>
    public static class TestServiceHelper
    {
        /// <summary>
        /// Builds a service provider with QuestionGeneratorService configured with MockOpenAIService
        /// </summary>
        public static ServiceProvider BuildQuestionGeneratorServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Use mock OpenAI service for deterministic testing
            var mockOpenAIService = new MockOpenAIService();
            services.AddSingleton<IOpenAIService>(mockOpenAIService);
            services.AddSingleton<IQuestionGeneratorService, QuestionGeneratorService>();

            return services.BuildServiceProvider();
        }
    }
}
