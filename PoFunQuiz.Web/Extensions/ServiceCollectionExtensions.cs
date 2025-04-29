using Microsoft.AspNetCore.Components.Server.Circuits;
using MudBlazor.Services;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Infrastructure.Services;
using PoFunQuiz.Web.Services;
using Azure.Data.Tables;

namespace PoFunQuiz.Web.Extensions
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
            
            // Add UI services
            services.AddUIServices();
            
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
                var connectionString = configuration.GetValue<string>("AzureTableStorage:ConnectionString"); 
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Azure Table Storage connection string ('AzureTableStorage:ConnectionString') not found or empty in configuration.");
                }
                return new TableServiceClient(connectionString);
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
        
        /// <summary>
        /// Adds UI-related services
        /// </summary>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // Add Blazor services with improved configuration
            services.AddRazorComponents()
                .AddInteractiveServerComponents(options => {
                    // Add more resilient SignalR configuration
                    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                    options.DisconnectedCircuitMaxRetained = 100;
                    options.DetailedErrors = true;
                });

            // Configure SignalR for more robust connections
            services.AddServerSideBlazor()
                .AddHubOptions(options => {
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
                });
            
            // Add a custom CircuitHandler to detect connection state changes
            services.AddSingleton<CircuitHandler, ConnectionStateHandler>();
            
            // Add application state services
            services.AddSingleton<GameState>();
            services.AddSingleton<ConnectionState>();
            
            // Add MudBlazor services
            services.AddMudServices();
            
            return services;
        }
    }
}