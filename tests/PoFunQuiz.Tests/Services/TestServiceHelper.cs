using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Web.Features.Quiz;

namespace PoFunQuiz.Tests;

/// <summary>
/// Helper class for building service providers in test classes
/// </summary>
public static class TestServiceHelper
{
    /// <summary>
    /// Builds a service provider with MockOpenAIService registered as IOpenAIService
    /// </summary>
    public static ServiceProvider BuildQuestionGeneratorServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<IOpenAIService>(new MockOpenAIService());
        return services.BuildServiceProvider();
    }
}
