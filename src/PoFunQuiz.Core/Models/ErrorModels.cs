using System;
using System.Collections.Generic;
using System.Net;

namespace PoFunQuiz.Core.Models;

public class ApiError
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();
    public string? StackTrace { get; set; }

    public static ApiError FromException(Exception ex, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiError
        {
            Message = ex.Message,
            Detail = ex.InnerException?.Message ?? string.Empty,
            StatusCode = statusCode,
            StackTrace = ex.StackTrace
        };
    }
}

public class ValidationError : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationError(Dictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}