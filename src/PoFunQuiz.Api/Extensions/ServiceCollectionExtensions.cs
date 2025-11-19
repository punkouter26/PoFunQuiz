using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Services;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PoFunQuiz.Server.Middleware; // Updated namespace
using PoFunQuiz.Server.Services; // Add this using statement

namespace PoFunQuiz.Server.Extensions
{
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

            // Add diagnostics services (server-side part)
            // services.AddScoped<OpenAIEndpointTester>(); // Moved to client

            return services;
        }

        /// <summary>
        /// Adds configuration services
        /// </summary>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            // Bind AppSettings for feature flags and other strongly-typed config
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            services.Configure<OpenAISettings>(configuration.GetSection("AzureOpenAI"));

            return services;
        }

        /// <summary>
        /// Adds storage services
        /// </summary>
        public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register TableServiceClient as a singleton
            services.AddSingleton(sp =>
            {
                var appSettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                if (string.IsNullOrEmpty(appSettings.Storage.TableStorageConnectionString))
                {
                    throw new InvalidOperationException("Azure Table Storage connection string is not configured.");
                }
                return new TableServiceClient(appSettings.Storage.TableStorageConnectionString);
            });

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
            services.AddSingleton<PoFunQuiz.Api.Features.Multiplayer.MultiplayerLobbyService>();

            return services;
        }

        // Removed AddUIServices as it's for Blazor Server and will be handled in the client project

        public static IServiceCollection AddAppConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind configuration to strongly-typed settings
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            return services;
        }

        public static IServiceCollection AddGlobalErrorHandling(this IServiceCollection services)
        {
            // Exception handling is configured via middleware in Program.cs
            // No service registration needed
            return services;
        }
    }
}
