using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using System.IO;

namespace PoFunQuiz.Server.Extensions
{
    /// <summary>
    /// Extension methods for configuring logging with structured telemetry
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configures Serilog for the application with structured logging and Application Insights integration
        /// </summary>
        public static IHostBuilder AddApplicationLogging(this IHostBuilder builder)
        {
            return builder.UseSerilog((context, services, configuration) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                
                // Get the solution root directory (one level up from the Web project)
                var rootDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\.."));
                var debugDirectory = Path.Combine(rootDirectory, "DEBUG");

                // Configure base logger with structured logging enrichers
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithProperty("Application", "PoFunQuiz")
                    .WriteTo.Console();

                // Add Application Insights sink if connection string is configured
                var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    configuration.WriteTo.ApplicationInsights(
                        appInsightsConnectionString,
                        new TraceTelemetryConverter());
                    
                    Log.Information("Application Insights structured logging enabled");
                }
                else
                {
                    Log.Warning("Application Insights connection string not found - telemetry will not be sent to Azure");
                }

                // Add file sink for development environment only
                if (isDevelopment)
                {
                    try
                    {
                        // Ensure DEBUG directory exists
                        if (!Directory.Exists(debugDirectory))
                        {
                            Directory.CreateDirectory(debugDirectory);
                        }

                        var logFilePath = Path.Combine(debugDirectory, "log.txt");

                        // Delete existing log file on startup for clean runs
                        if (File.Exists(logFilePath))
                        {
                            try
                            {
                                File.Delete(logFilePath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Could not delete existing log file: {ex.Message}");
                            }
                        }

                        configuration.WriteTo.File(
                            new CompactJsonFormatter(),
                            path: logFilePath,
                            rollOnFileSizeLimit: false,
                            shared: false);
                        
                        Log.Information("Development file logging enabled at {LogPath}", logFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Could not configure file logging");
                    }
                }
            });
        }
    }
}
