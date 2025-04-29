using Serilog;
using Serilog.Events;

namespace PoFunQuiz.Web.Extensions
{
    /// <summary>
    /// Extension methods for configuring logging
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configures Serilog for the application
        /// </summary>
        public static IHostBuilder AddApplicationLogging(this IHostBuilder builder)
        {
            // Create a new log file for each run
            var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logFileName, 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            // Use Serilog for the host application
            return builder.UseSerilog();
        }
    }
}