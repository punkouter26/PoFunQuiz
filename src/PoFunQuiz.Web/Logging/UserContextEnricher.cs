using Serilog.Core;
using Serilog.Events;

namespace PoFunQuiz.Web.Logging;

/// <summary>
/// Serilog enricher that adds UserId, SessionId, and Environment to every log event globally —
/// including background services, SignalR hub events, and startup logs, not just HTTP request logs.
/// Falls back gracefully when no HTTP context is available (e.g., hosted services).
/// Pattern: GoF Decorator — wraps log events with ambient context without modifying callers.
/// </summary>
public sealed class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public UserContextEnricher(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;

        // UserId — authenticated user name or connection ID as fallback
        var userId = context?.User?.Identity?.Name
            ?? context?.Connection.Id
            ?? "system";

        // SessionId — ASP.NET trace identifier (unique per request) or fallback
        var sessionId = context?.TraceIdentifier ?? "no-request-context";

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SessionId", sessionId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("EnvironmentName", _environment.EnvironmentName));
    }
}
