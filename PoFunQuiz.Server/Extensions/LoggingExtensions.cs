using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
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
            var debugDirectory = Path.Combine(rootDirectory, "DEBUG");

            // Ensure DEBUG directory exists
            try
            {
                if (!Directory.Exists(debugDirectory))
                {
                    Directory.CreateDirectory(debugDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create DEBUG directory: {ex.Message}");
            }

            var logFilePath = Path.Combine(debugDirectory, "log.txt");

            // Delete existing log file if it exists to create a new one each run (overwrite on startup)
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

            // Configure Serilog to emit structured JSON to file for easier parsing
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(new CompactJsonFormatter(),
                    path: logFilePath,
                    rollOnFileSizeLimit: false,
                    shared: false)
                .CreateLogger();

            // Use Serilog for the host application
            return builder.UseSerilog();
        }
    }
}
