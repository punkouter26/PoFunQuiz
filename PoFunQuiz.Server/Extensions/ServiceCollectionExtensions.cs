using Microsoft.Extensions.Options;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Services;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PoFunQuiz.Server.Middleware; // Updated namespace

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
            services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
            services.Configure<TableStorageSettings>(configuration.GetSection("AzureTableStorage"));
            
            // Register configuration service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
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
                var tableStorageSettings = sp.GetRequiredService<IOptions<TableStorageSettings>>().Value;
                if (string.IsNullOrEmpty(tableStorageSettings.ConnectionString))
                {
                    throw new InvalidOperationException("Azure Table Storage connection string is not configured.");
                }
                return new TableServiceClient(tableStorageSettings.ConnectionString);
            });
            
            // Register storage services
            services.AddScoped<IPlayerStorageService, PlayerStorageService>();
            services.AddScoped<IGameSessionService, GameSessionService>();
            
            return services;
        }
        
        /// <summary>
        /// Adds business services
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Register business services
            services.AddScoped<IQuestionGeneratorService, OpenAIQuestionGeneratorService>();
            
            return services;
        }
        
        // Removed AddUIServices as it's for Blazor Server and will be handled in the client project

        public static IServiceCollection AddAppConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind configuration to strongly-typed settings
            services.Configure<AppSettings>(configuration);
            
            // Register configuration service
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            return services;
        }

        public static IServiceCollection AddGlobalErrorHandling(this IServiceCollection services)
        {
            services.AddTransient<GlobalExceptionMiddleware>();
            return services;
        }
    }
}
