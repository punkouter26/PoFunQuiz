using System;

namespace PoFunQuiz.Core.Configuration;

public class AppSettings
{
    public StorageSettings Storage { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public DiagnosticsSettings Diagnostics { get; set; } = new();
}

public class StorageSettings
{
    public string TableStorageConnectionString { get; set; } = string.Empty;
    public string BlobStorageConnectionString { get; set; } = string.Empty;
    public string DefaultContainer { get; set; } = "quiz-data";
}

public class AuthSettings
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class LoggingSettings
{
    public string ApplicationInsightsKey { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
}

public class DiagnosticsSettings
{
    public bool EnableDetailedErrors { get; set; } = false;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public string[] MonitoredEndpoints { get; set; } = Array.Empty<string>();
} 