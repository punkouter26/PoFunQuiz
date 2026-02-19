using PoFunQuiz.Web.Extensions;
using PoFunQuiz.Web.Middleware;
using PoFunQuiz.Web.Features.Quiz;
using PoFunQuiz.Web.Logging;
using Serilog;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using System.Text.Json;
using Scalar.AspNetCore;
using Radzen;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Azure.Data.Tables;

// ============================================================
// Bootstrap logger for early startup logs
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting PoFunQuiz web application");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // Ports: Use 5000/5001 locally; let Azure App Service set ports via ASPNETCORE_URLS
    // ============================================================
    if (builder.Environment.IsDevelopment())
    {
        builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");
    }

    // Load static web assets from NuGet packages (Radzen CSS/JS, Blazor framework, scoped CSS).
    // Needed when running via 'dotnet run' outside of the published output so that
    // /_content/**, /_framework/**, and fingerprinted app CSS are served correctly.
    builder.WebHost.UseStaticWebAssets();

    // ============================================================
    // Azure Key Vault configuration (Managed Identity / DefaultAzureCredential)
    // ============================================================
    var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
    if (!string.IsNullOrWhiteSpace(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new Azure.Identity.DefaultAzureCredential());
        Log.Information("Configured Azure Key Vault: {KeyVaultEndpoint}", keyVaultEndpoint);

        // Map Key Vault secret names (hyphens) to ASP.NET config paths (colons)
        var cfg = builder.Configuration;
        void MapSecret(string secretName, params string[] paths)
        {
            var value = cfg[secretName];
            if (string.IsNullOrEmpty(value)) return;
            foreach (var path in paths) cfg[path] = value;
            Log.Information("Mapped Key Vault secret {SecretName}", secretName);
        }

        MapSecret("AzureOpenAI-ApiKey",              "AzureOpenAI:ApiKey");
        MapSecret("AzureOpenAI-Endpoint",            "AzureOpenAI:Endpoint");
        MapSecret("AzureOpenAI-DeploymentName",      "AzureOpenAI:DeploymentName");
        MapSecret("ApplicationInsights-ConnectionString", "ApplicationInsights:ConnectionString");
        MapSecret("PoFunQuiz-TableStorageConnectionString", "ConnectionStrings:tables");
        MapSecret("PoFunQuiz-SignalRConnectionString", "Azure:SignalR:ConnectionString");
    }
    else
    {
        Log.Warning("AZURE_KEY_VAULT_ENDPOINT not configured; skipping Key Vault integration");
    }

    // ============================================================
    // Serilog (structured logging → Console, File, App Insights)
    // ============================================================
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        // UserContextEnricher adds UserId, SessionId, EnvironmentName to ALL log events
        // (not just HTTP request logs), including SignalR, background services, and startup.
        var enricher = new UserContextEnricher(
            services.GetRequiredService<IHttpContextAccessor>(),
            services.GetRequiredService<IWebHostEnvironment>());

        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithCorrelationId()
            .Enrich.With(enricher)
            .Enrich.WithProperty("Application", "PoFunQuiz")
            .Destructure.ToMaximumDepth(5)
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumCollectionCount(10)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/pofunquiz-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatter: new Serilog.Formatting.Compact.CompactJsonFormatter(),
                fileSizeLimitBytes: 50_000_000,
                rollOnFileSizeLimit: true);


    });

    // ============================================================
    // OpenTelemetry: Traces, Metrics, Logs → OTLP / Azure Monitor
    // ============================================================
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    var otelBuilder = builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
            tracing
                .AddSource("PoFunQuiz")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        });

    // Export to OTLP if endpoint is configured (e.g., local collector)
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        otelBuilder
            .WithMetrics(m => m.AddOtlpExporter())
            .WithTracing(t => t.AddOtlpExporter());
    }

    // Azure Monitor OpenTelemetry exporter — aggregates OTel traces, metrics, and logs to Application Insights
    // This is the recommended approach per Microsoft docs; the legacy SDK is kept only for TelemetryClient injection.
    var appInsightsConnStr = builder.Configuration["ApplicationInsights:ConnectionString"]
        ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (!string.IsNullOrWhiteSpace(appInsightsConnStr))
    {
        otelBuilder.UseAzureMonitor(options => options.ConnectionString = appInsightsConnStr);
        Log.Information("Configured Azure Monitor OpenTelemetry exporter");
    }
    else
    {
        Log.Warning("ApplicationInsights:ConnectionString not set; Azure Monitor OTel exporter skipped");
    }

    // ============================================================
    // OpenAPI / Scalar API documentation
    // ============================================================
    builder.Services.AddOpenApi();

    // ============================================================
    // Core web infrastructure
    // ============================================================
    builder.Services.AddAntiforgery();
    builder.Services.AddAuthorization();
    builder.Services.AddHttpClient();

    // IHttpContextAccessor — required by UserContextEnricher to access ambient HTTP context
    builder.Services.AddHttpContextAccessor();

    // Output caching — reduces Azure OpenAI calls for identical quiz topic+count requests
    builder.Services.AddOutputCache(options =>
    {
        // Quiz questions: cache 60 s, vary by count and category query params
        options.AddPolicy("QuizQuestions", policy =>
            policy.Expire(TimeSpan.FromSeconds(60))
                  .SetVaryByQuery("count", "category")
                  .Tag("quiz"));
    });

    // Blazor Interactive Server components
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Radzen UI library
    builder.Services.AddRadzenComponents();

    // ============================================================
    // Health Checks: /health verifies all APIs and databases
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck<PoFunQuiz.Web.HealthChecks.TableStorageHealthCheck>("table_storage")
        .AddCheck<PoFunQuiz.Web.HealthChecks.OpenAIHealthCheck>("openai");

    // ============================================================
    // SignalR (local or Azure SignalR Service)
    // ============================================================
    var signalRBuilder = builder.Services.AddSignalR();
    var signalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
    if (!string.IsNullOrEmpty(signalRConnectionString))
    {
        signalRBuilder.AddAzureSignalR();
        Log.Information("Configured Azure SignalR Service");
    }
    else
    {
        Log.Warning("Azure SignalR not configured; using local SignalR");
    }

    // ============================================================
    // Azure Table Storage client (direct connection, no Aspire)
    // ============================================================
    var tableStorageConnectionString = builder.Configuration.GetConnectionString("tables")
        ?? builder.Configuration["AppSettings:Storage:TableStorageConnectionString"]
        ?? "UseDevelopmentStorage=true";

    builder.Services.AddSingleton(new TableServiceClient(tableStorageConnectionString));

    // ============================================================
    // Application services (DI registration via extension methods)
    // ============================================================
    builder.Services.AddApplicationServices(builder.Configuration);

    // Scoped state services for Interactive Server components
    builder.Services.AddScoped<PoFunQuiz.Web.GameState>();

    var app = builder.Build();

    // ============================================================
    // Middleware pipeline
    // ============================================================
    app.UseGlobalExceptionHandler();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId",
                httpContext.User?.Identity?.Name ?? httpContext.Connection.Id);
            diagnosticContext.Set("SessionId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("UserAgent",
                httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseContentSecurityPolicy(app.Environment.IsDevelopment());

    app.UseOutputCache();

    app.UseAntiforgery();
    app.MapStaticAssets();

    // OpenAPI + Scalar UI
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Vertical Slice API Endpoints (Quiz questions)
    app.MapQuizEndpoints();

    // /health endpoint — JSON response with all check statuses
    app.MapJsonHealthChecks();

    // ============================================================
    // /diag endpoint — exposes connection info with masked secrets
    // ============================================================
    app.MapGet("/api/diag", (IConfiguration config) =>
    {
        // Helper to mask the middle of sensitive values for security
        static string Mask(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "(not set)";
            if (value.Length <= 8) return new string('*', value.Length);
            return string.Concat(value.AsSpan(0, 4), "****", value.AsSpan(value.Length - 4));
        }

        return Results.Json(new
        {
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow,
            connections = new
            {
                tableStorage = Mask(config.GetConnectionString("tables")
                    ?? config["AppSettings:Storage:TableStorageConnectionString"]),
                azureSignalR = Mask(config["Azure:SignalR:ConnectionString"]),
                applicationInsights = Mask(config["ApplicationInsights:ConnectionString"]),
                keyVault = Mask(config["AZURE_KEY_VAULT_ENDPOINT"])
            },
            azureOpenAI = new
            {
                endpoint = Mask(config["AzureOpenAI:Endpoint"]),
                apiKey = Mask(config["AzureOpenAI:ApiKey"]),
                deploymentName = config["AzureOpenAI:DeploymentName"] ?? "(not set)"
            },
            settings = new
            {
                urls = builder.WebHost.GetSetting("urls") ?? "default",
                contentRoot = app.Environment.ContentRootPath
            }
        });
    })
    .WithName("Diagnostics")
    .WithOpenApi()
    .WithTags("Diagnostics");

    // ============================================================
    // SignalR Hub
    // ============================================================
    app.MapHub<PoFunQuiz.Web.Features.Multiplayer.GameHub>("/gamehub");

    // ============================================================
    // Vertical Slice API Endpoints
    // ============================================================
    PoFunQuiz.Web.Features.Leaderboard.GetLeaderboard.MapEndpoint(app);
    PoFunQuiz.Web.Features.Leaderboard.SubmitScore.MapEndpoint(app);

    // ============================================================
    // Blazor Interactive Server components
    // ============================================================
    app.MapRazorComponents<PoFunQuiz.Web.Components.App>()
        .AddInteractiveServerRenderMode();

    app.UseLifetimeLogging();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class accessible to integration tests
public partial class Program { }
