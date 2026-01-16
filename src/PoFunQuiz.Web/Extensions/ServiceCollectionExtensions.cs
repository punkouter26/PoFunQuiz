using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Services;
using Azure.Data.Tables;
using PoFunQuiz.Web.Services;

namespace PoFunQuiz.Web.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the service collection
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration services
        services.AddConfigurationServices(configuration);

        // Add storage services
        services.AddStorageServices(configuration);

        // Add business services
        services.AddBusinessServices();

        return services;
    }

    /// <summary>
    /// Adds configuration services
    /// </summary>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<OpenAISettings>(configuration.GetSection("AzureOpenAI"));

        return services;
    }

    /// <summary>
    /// Adds storage services (TableServiceClient is registered via Aspire in Program.cs)
    /// </summary>
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        // TableServiceClient is registered by Aspire's AddAzureTableClient in Program.cs
        // which automatically connects to Azurite in development

        // Register TableClient for specific table (Scoped)
        services.AddScoped<TableClient>(sp =>
        {
            var serviceClient = sp.GetRequiredService<TableServiceClient>();
            return serviceClient.GetTableClient("PoFunQuizPlayers");
        });

        // Register storage services
        services.AddScoped<IPlayerStorageService, PlayerStorageService>();
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<PoFunQuiz.Core.Interfaces.ILeaderboardRepository, LeaderboardRepository>();

        return services;
    }

    /// <summary>
    /// Adds business services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Register hosted services
        services.AddHostedService<TableStorageInitializer>();

        // Register OpenAI question deserializers (Chain of Responsibility pattern)
        services.AddSingleton<IQuizQuestionDeserializer, SchemaWrapperDeserializer>();
        services.AddSingleton<IQuizQuestionDeserializer, DirectArrayDeserializer>();
        services.AddSingleton<IQuizQuestionDeserializer, SingleObjectDeserializer>();

        // Register business services
        services.AddScoped<IQuestionGeneratorService, QuestionGeneratorService>();
        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddSingleton<PoFunQuiz.Web.Features.Multiplayer.MultiplayerLobbyService>();

        // Register client services for interactive components
        services.AddScoped<GameClientService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();

        return services;
    }
}
