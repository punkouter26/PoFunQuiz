using System.Net;
using System.Text.Json;

namespace PoFunQuiz.Web.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions globally
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FileNotFoundException ex)
            {
                // Static asset files not found should return 404, not 500.
                // This happens when static web assets (Radzen CSS/JS, Blazor framework)
                // are requested but the file doesn't physically exist on disk.
                _logger.LogWarning(ex, "Static asset not found: {Path}", context.Request.Path);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                // Also ensure the exception is captured by Serilog so it appears in structured logs
                Serilog.Log.Error(ex, "Unhandled exception occurred in HTTP request pipeline");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ErrorResponse response;

            if (_env.IsDevelopment())
            {
                response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }
            else
            {
                response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "An internal server error occurred. Please try again later."
                };
            }

            var jsonResponse = JsonSerializer.Serialize(response);

            // Log the error to Serilog for centralized structured logging (avoid throwing from here)
            try
            {
                Serilog.Log.Error(exception, "Handled exception producing error response");
            }
            catch { /* swallow to avoid secondary failures */ }

            return context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// Error response class for consistent error handling
        /// </summary>
        private sealed class ErrorResponse
        {
            public int StatusCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? StackTrace { get; set; }
            public string? InnerException { get; set; }
        }
    }

    // Extension methods for the middleware
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
