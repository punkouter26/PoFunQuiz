using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace PoFunQuiz.Client.Services
{
    /// <summary>
    /// Client-side logging service that sends logs to the server
    /// </summary>
    public interface IClientLogger
    {
        Task LogInformation(string message, string? page = null, string? component = null);
        Task LogWarning(string message, string? page = null, string? component = null);
        Task LogError(string message, Exception? exception = null, string? page = null, string? component = null);
        Task LogDebug(string message, string? page = null, string? component = null);
    }

    public class ClientLogger : IClientLogger
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;

        public ClientLogger(HttpClient httpClient, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
        }

        public async Task LogInformation(string message, string? page = null, string? component = null)
        {
            await SendLog("Information", message, null, page, component);
        }

        public async Task LogWarning(string message, string? page = null, string? component = null)
        {
            await SendLog("Warning", message, null, page, component);
        }

        public async Task LogError(string message, Exception? exception = null, string? page = null, string? component = null)
        {
            var stackTrace = exception?.StackTrace ?? exception?.ToString();
            await SendLog("Error", message, stackTrace, page, component);
        }

        public async Task LogDebug(string message, string? page = null, string? component = null)
        {
            await SendLog("Debug", message, null, page, component);
        }

        private async Task SendLog(string level, string message, string? stackTrace, string? page, string? component)
        {
            try
            {
                var currentPage = page ?? _navigationManager.Uri;
                var userAgent = await GetUserAgent();

                var logEntry = new
                {
                    Level = level,
                    Message = message,
                    Page = currentPage,
                    Component = component,
                    StackTrace = stackTrace,
                    UserAgent = userAgent,
                    UserId = "Anonymous" // Could be enhanced with authentication
                };

                // Fire and forget - don't await to avoid blocking UI
                _ = _httpClient.PostAsJsonAsync("/api/log/client", logEntry);
            }
            catch
            {
                // Silently fail - logging shouldn't break the app
                Console.WriteLine($"[ClientLogger] Failed to send log: {message}");
            }
        }

        private async Task<string?> GetUserAgent()
        {
            try
            {
                // In a real implementation, you might use JSInterop to get navigator.userAgent
                // For now, return a simple identifier
                return "Blazor WebAssembly Client";
            }
            catch
            {
                return null;
            }
        }
    }
}
