using Microsoft.Extensions.Options;

namespace PoFunQuiz.Server.Middleware
{
    public class FrontendSelectorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FrontendSelectorMiddleware> _logger;

        public FrontendSelectorMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<FrontendSelectorMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // For Blazor, leave paths as-is (default behavior)
            // All React-related logic has been removed
            await _next(context);
        }
    }
}
