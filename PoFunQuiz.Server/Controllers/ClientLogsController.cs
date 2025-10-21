using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace PoFunQuiz.Server.Controllers
{
    /// <summary>
    /// API endpoint for client-side logs
    /// </summary>
    [ApiController]
    [Route("api/log")]
    public class ClientLogsController : ControllerBase
    {
        private readonly ILogger<ClientLogsController> _logger;
        private readonly TelemetryClient _telemetryClient;

        public ClientLogsController(
            ILogger<ClientLogsController> logger,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Receives client-side logs and forwards them to server logging infrastructure
        /// </summary>
        /// <param name="logEntry">The client log entry</param>
        [HttpPost("client")]
        public IActionResult LogClientMessage([FromBody] ClientLogEntry logEntry)
        {
            if (logEntry == null || string.IsNullOrEmpty(logEntry.Message))
            {
                return BadRequest("Log entry is required");
            }

            // Determine log level
            var logLevel = ParseLogLevel(logEntry.Level);

            // Create structured log with client context
            _logger.Log(
                logLevel,
                "Client Log: {Message} | Page: {Page} | Component: {Component} | User: {User}",
                logEntry.Message,
                logEntry.Page ?? "Unknown",
                logEntry.Component ?? "Unknown",
                logEntry.UserId ?? "Anonymous");

            // Track custom event in Application Insights with properties
            var eventTelemetry = new EventTelemetry("ClientLog")
            {
                Timestamp = DateTimeOffset.UtcNow
            };
            
            eventTelemetry.Properties.Add("Level", logEntry.Level ?? "Information");
            eventTelemetry.Properties.Add("Message", logEntry.Message);
            eventTelemetry.Properties.Add("Page", logEntry.Page ?? "Unknown");
            eventTelemetry.Properties.Add("Component", logEntry.Component ?? "Unknown");
            eventTelemetry.Properties.Add("UserId", logEntry.UserId ?? "Anonymous");
            
            if (!string.IsNullOrEmpty(logEntry.StackTrace))
            {
                eventTelemetry.Properties.Add("StackTrace", logEntry.StackTrace);
            }
            
            if (!string.IsNullOrEmpty(logEntry.UserAgent))
            {
                eventTelemetry.Properties.Add("UserAgent", logEntry.UserAgent);
            }

            _telemetryClient.TrackEvent(eventTelemetry);

            return Ok(new { success = true, timestamp = DateTime.UtcNow });
        }

        private static LogLevel ParseLogLevel(string? level)
        {
            return level?.ToLowerInvariant() switch
            {
                "debug" => LogLevel.Debug,
                "information" or "info" => LogLevel.Information,
                "warning" or "warn" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "critical" => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }
    }

    /// <summary>
    /// Client log entry model
    /// </summary>
    public class ClientLogEntry
    {
        public string? Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Page { get; set; }
        public string? Component { get; set; }
        public string? UserId { get; set; }
        public string? StackTrace { get; set; }
        public string? UserAgent { get; set; }
    }
}
