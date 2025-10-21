# Application Insights Monitoring Guide

This guide provides essential KQL (Kusto Query Language) queries for monitoring PoFunQuiz application health and performance.

## Prerequisites

1. Deploy the application to Azure using `azd up`
2. Application Insights will be automatically configured
3. Access queries from Azure Portal → Application Insights → Logs

---

## Essential KQL Queries

### 1. User Activity Over Last 7 Days

**Purpose:** Track daily active users and quiz generation patterns

```kql
// User Activity - Last 7 Days
customEvents
| where timestamp >= ago(7d)
| where name in ("QuizGeneration", "QuizGenerationInCategory")
| summarize 
    TotalRequests = count(),
    UniqueUsers = dcount(user_Id),
    SuccessfulGenerations = countif(customDimensions.Success == "true"),
    FailedGenerations = countif(customDimensions.Success == "false")
    by bin(timestamp, 1d)
| order by timestamp desc
| project 
    Date = format_datetime(timestamp, 'yyyy-MM-dd'),
    TotalRequests,
    UniqueUsers,
    SuccessfulGenerations,
    FailedGenerations,
    SuccessRate = round(100.0 * SuccessfulGenerations / TotalRequests, 2)
```

**Usage:** Run this query to identify:
- Peak usage days
- User engagement trends
- Success rate over time
- Potential issues causing failures

---

### 2. Top 10 Slowest Requests

**Purpose:** Identify performance bottlenecks in the application

```kql
// Top 10 Slowest Requests - All Time
requests
| where timestamp >= ago(7d)
| where success == true
| top 10 by duration desc
| project 
    Timestamp = format_datetime(timestamp, 'yyyy-MM-dd HH:mm:ss'),
    Name = name,
    URL = url,
    DurationMs = round(duration, 2),
    ResultCode = resultCode,
    OperationId = operation_Id
| order by DurationMs desc
```

**Advanced Version with Question Generation Metrics:**

```kql
// Slow Question Generation Operations
customEvents
| where timestamp >= ago(7d)
| where name in ("QuizGeneration", "QuizGenerationInCategory")
| where isnotnull(customMeasurements.GenerationDurationMs)
| extend DurationMs = todouble(customMeasurements.GenerationDurationMs)
| top 10 by DurationMs desc
| project 
    Timestamp = format_datetime(timestamp, 'yyyy-MM-dd HH:mm:ss'),
    EventType = name,
    Category = tostring(customDimensions.Category),
    QuestionCount = toint(customDimensions.QuestionCount),
    DurationMs = round(DurationMs, 2),
    Success = tostring(customDimensions.Success)
| order by DurationMs desc
```

**Usage:** Use this to:
- Identify slow API endpoints
- Detect performance degradation
- Prioritize optimization efforts
- Monitor impact of code changes

---

### 3. Error Rate Over Last 24 Hours

**Purpose:** Monitor application health and detect issues in real-time

```kql
// Error Rate - Last 24 Hours (Hourly Breakdown)
union 
    (requests 
    | where timestamp >= ago(24h)
    | summarize 
        TotalRequests = count(),
        FailedRequests = countif(success == false)
        by bin(timestamp, 1h)
    | extend Source = "HTTP Requests"
    ),
    (exceptions
    | where timestamp >= ago(24h)
    | summarize 
        TotalExceptions = count()
        by bin(timestamp, 1h)
    | extend Source = "Exceptions"
    | extend TotalRequests = 0, FailedRequests = TotalExceptions
    ),
    (customEvents
    | where timestamp >= ago(24h)
    | where name in ("QuizGeneration", "QuizGenerationInCategory", "ClientLog")
    | summarize 
        TotalEvents = count(),
        FailedEvents = countif(customDimensions.Success == "false" or customDimensions.Level == "Error")
        by bin(timestamp, 1h)
    | extend Source = "Custom Events"
    | extend TotalRequests = TotalEvents, FailedRequests = FailedEvents
    )
| summarize 
    TotalOperations = sum(TotalRequests),
    TotalFailures = sum(FailedRequests)
    by bin(timestamp, 1h), Source
| extend ErrorRate = round(100.0 * TotalFailures / TotalOperations, 2)
| order by timestamp desc
| project 
    Hour = format_datetime(timestamp, 'yyyy-MM-dd HH:mm'),
    Source,
    TotalOperations,
    TotalFailures,
    ErrorRate = strcat(ErrorRate, "%")
```

**Simplified Version for Quick Health Check:**

```kql
// Simple Error Rate - Last 24 Hours
requests
| where timestamp >= ago(24h)
| summarize 
    Total = count(),
    Errors = countif(success == false)
    by bin(timestamp, 1h)
| extend ErrorRate = round(100.0 * Errors / Total, 2)
| order by timestamp desc
| project 
    Hour = format_datetime(timestamp, 'HH:mm'),
    Total,
    Errors,
    ErrorRate = strcat(ErrorRate, "%")
```

**Usage:** Monitor for:
- Sudden spikes in error rate (potential incidents)
- Gradual increases (code quality issues)
- Patterns (time-based failures)
- Compare error sources (client vs. server)

---

## Client-Side Logging Queries

### View Client Logs

```kql
// Client-Side Logs - Last Hour
customEvents
| where timestamp >= ago(1h)
| where name == "ClientLog"
| project 
    Timestamp = format_datetime(timestamp, 'yyyy-MM-dd HH:mm:ss'),
    Level = tostring(customDimensions.Level),
    Message = tostring(customDimensions.Message),
    Page = tostring(customDimensions.Page),
    Component = tostring(customDimensions.Component),
    UserAgent = tostring(customDimensions.UserAgent)
| order by Timestamp desc
```

---

## Alerts Configuration

### Recommended Alerts

1. **High Error Rate Alert**
   - Query: Error rate > 5% for 15 minutes
   - Action: Send email/SMS to dev team

2. **Slow Performance Alert**
   - Query: P95 request duration > 2000ms for 10 minutes
   - Action: Create work item

3. **No Activity Alert**
   - Query: Zero requests for 1 hour (during business hours)
   - Action: Check if service is down

---

## Structured Logging Examples

### In C# Controllers

```csharp
// Example: Structured logging with properties
_logger.LogInformation(
    "User {UserId} generated {Count} questions in category {Category}",
    userId,
    questionCount,
    category);

// Custom telemetry event
var eventTelemetry = new EventTelemetry("QuizGeneration");
eventTelemetry.Properties.Add("Category", category);
eventTelemetry.Metrics.Add("Duration", stopwatch.ElapsedMilliseconds);
_telemetryClient.TrackEvent(eventTelemetry);
```

### In Blazor Client

```csharp
// Example: Client-side logging
await _clientLogger.LogInformation(
    "Quiz started with 10 questions", 
    page: "/quiz", 
    component: "QuizPage");

await _clientLogger.LogError(
    "Failed to load questions", 
    exception: ex,
    page: "/quiz");
```

---

## Best Practices

1. **Use Structured Logging:** Always use property placeholders `{PropertyName}` instead of string interpolation
2. **Add Context:** Include relevant properties (userId, category, count, etc.)
3. **Track Metrics:** Use `TrackMetric` for performance measurements
4. **Custom Events:** Use `TrackEvent` for business events (quiz started, completed, etc.)
5. **Client Logs:** Send critical client errors to server for centralized monitoring
6. **Avoid Over-Logging:** Don't log sensitive data (passwords, tokens, PII)

---

## Troubleshooting

### No Data in Application Insights?

1. Check connection string in `appsettings.json`
2. Verify Application Insights resource is created in Azure
3. Wait 2-5 minutes for telemetry to appear
4. Check browser console for client-side errors

### Logs Not Appearing in DEBUG Folder?

1. Verify you're running in Development mode
2. Check `DEBUG/log.txt` exists in solution root
3. File logging only enabled for Development environment

---

## Additional Resources

- [Application Insights Overview](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [KQL Quick Reference](https://learn.microsoft.com/azure/data-explorer/kql-quick-reference)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Writing-Log-Events)
