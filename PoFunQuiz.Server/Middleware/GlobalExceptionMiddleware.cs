using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PoFunQuiz.Core.Models;

namespace PoFunQuiz.Server.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var apiError = exception switch
        {
            ValidationError validationError => new ApiError
            {
                Message = "Validation failed",
                ValidationErrors = validationError.Errors,
                StatusCode = HttpStatusCode.BadRequest
            },
            NotFoundException => new ApiError
            {
                Message = exception.Message,
                StatusCode = HttpStatusCode.NotFound
            },
            UnauthorizedException => new ApiError
            {
                Message = exception.Message,
                StatusCode = HttpStatusCode.Unauthorized
            },
            _ => ApiError.FromException(exception)
        };

        response.StatusCode = (int)apiError.StatusCode;

#if DEBUG
        apiError.StackTrace = exception.StackTrace;
#endif

        var result = JsonSerializer.Serialize(apiError, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}
