using Serilog;
using Serilog.Events;
using System.IO;

namespace PoFunQuiz.Server.Extensions
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
            // Get the solution root directory (one level up from the Web project)
            var rootDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\.."));
            var logFilePath = Path.Combine(rootDirectory, "log.txt");
            
            // Delete existing log file if it exists to create a new one each run
            if (File.Exists(logFilePath))
            {
                try
                {
                    File.Delete(logFilePath);
                }
                catch (Exception ex)
                {
                    // Log to console that we couldn't delete the file but will continue using it
                    Console.WriteLine($"Warning: Could not delete existing log file: {ex.Message}");
                    // We'll continue and overwrite the existing file rather than creating a timestamped version
                }
            }
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, 
                    rollOnFileSizeLimit: false, // Don't roll on file size
                    shared: false, // Don't share the file
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            // Use Serilog for the host application
            return builder.UseSerilog();
        }
    }
}
