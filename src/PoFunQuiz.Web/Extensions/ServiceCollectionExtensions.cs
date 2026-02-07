using Microsoft.Extensions.Options;
using PoFunQuiz.Web.Configuration;
using PoFunQuiz.Web.Features.Quiz;
using PoFunQuiz.Web.Features.Leaderboard;
using PoFunQuiz.Web.Features.Multiplayer;
using PoFunQuiz.Web.Features.Storage;
using Azure.Data.Tables;

namespace PoFunQuiz.Web.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration (Options pattern)
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<OpenAISettings>(configuration.GetSection("AzureOpenAI"));

        // Storage
        services.AddScoped<TableClient>(sp =>
        {
            var serviceClient = sp.GetRequiredService<TableServiceClient>();
            return serviceClient.GetTableClient("PoFunQuizPlayers");
        });
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddHostedService<TableStorageInitializer>();

        // Business services
        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddSingleton<MultiplayerLobbyService>();
        services.AddScoped<GameClientService>();

        return services;
    }
}
