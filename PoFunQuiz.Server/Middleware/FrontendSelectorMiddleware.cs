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
            var frontendType = _configuration["Frontend:Type"] ?? "Blazor";
            
            // Only handle requests for static files and the root path
            if (context.Request.Path == "/" || 
                context.Request.Path.StartsWithSegments("/static") ||
                context.Request.Path.Value?.EndsWith(".js") == true ||
                context.Request.Path.Value?.EndsWith(".css") == true ||
                context.Request.Path.Value?.EndsWith(".html") == true ||
                context.Request.Path.Value?.EndsWith(".png") == true ||
                context.Request.Path.Value?.EndsWith(".ico") == true ||
                context.Request.Path.Value?.EndsWith(".json") == true)
            {
                if (frontendType.Equals("React", StringComparison.OrdinalIgnoreCase))
                {
                    // For React, serve from the react-client build folder
                    var reactPath = _configuration["Frontend:ReactPath"] ?? "react-client";
                    
                    if (context.Request.Path == "/")
                    {
                        context.Request.Path = $"/{reactPath}/index.html";
                    }
                    else if (context.Request.Path.StartsWithSegments("/static"))
                    {
                        // React static files are usually under /static
                        var originalPath = context.Request.Path.Value;
                        context.Request.Path = $"/{reactPath}{originalPath}";
                    }
                    else
                    {
                        // Other files
                        var originalPath = context.Request.Path.Value;
                        context.Request.Path = $"/{reactPath}{originalPath}";
                    }
                    
                    _logger.LogDebug("Redirecting to React frontend: {Path}", context.Request.Path);
                }
                // For Blazor, leave paths as-is (default behavior)
            }

            await _next(context);
        }
    }
}
