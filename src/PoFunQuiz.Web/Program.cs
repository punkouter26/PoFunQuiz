using PoFunQuiz.Web.Extensions;
using PoFunQuiz.Web.Middleware;
using PoFunQuiz.Web.Features.Quiz;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

        // Map Key Vault secrets (hyphens) to configuration paths (colons)
        MapKeyVaultSecrets(builder.Configuration);
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
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithCorrelationId()
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

        var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            configuration.WriteTo.ApplicationInsights(
                appInsightsConnectionString,
                new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
        }
    });

    // ============================================================
    // OpenTelemetry: Traces, Metrics, Logs → OTLP / Azure Monitor
    // ============================================================
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
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
        builder.Services.AddOpenTelemetry()
            .WithMetrics(m => m.AddOtlpExporter())
            .WithTracing(t => t.AddOtlpExporter());
    }

    // Application Insights SDK (for TelemetryClient usage in controllers)
    builder.Services.AddApplicationInsightsTelemetry();

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
        .AddCheck<PoFunQuiz.Web.HealthChecks.OpenAIHealthCheck>("openai")
        .AddCheck<PoFunQuiz.Web.HealthChecks.InternetConnectivityHealthCheck>("internet");

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

    app.UseAntiforgery();
    app.MapStaticAssets();

    // OpenAPI + Scalar UI
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Vertical Slice API Endpoints (Quiz questions)
    app.MapQuizEndpoints();

    // ============================================================
    // /health endpoint — JSON response with all check statuses
    // ============================================================
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    exception = e.Value.Exception?.Message
                })
            });
            await context.Response.WriteAsync(result);
        }
    });

    // ============================================================
    // /diag endpoint — exposes connection info with masked secrets
    // ============================================================
    app.MapGet("/diag", (IConfiguration config) =>
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

    // ============================================================
    // Application lifecycle logging
    // ============================================================
    var lifetime = app.Lifetime;
    lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Services.GetService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            ?.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;
        if (addresses != null)
        {
            foreach (var address in addresses)
            {
                Log.Information("Now listening on: {Address}", address);
            }
        }
        Log.Information("Application started");
    });
    lifetime.ApplicationStopping.Register(() => Log.Information("Application is stopping"));
    lifetime.ApplicationStopped.Register(() => Log.Information("Application stopped"));

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

// ============================================================
// Helper: Map Key Vault secret names (hyphens) to config paths (colons)
// ============================================================
static void MapKeyVaultSecrets(IConfigurationManager config)
{
    var mappings = new Dictionary<string, string[]>
    {
        // Shared secrets (used by multiple apps) — no app prefix
        ["AzureOpenAI-ApiKey"] = ["AzureOpenAI:ApiKey", "AppSettings:AzureOpenAI:ApiKey"],
        ["AzureOpenAI-Endpoint"] = ["AzureOpenAI:Endpoint", "AppSettings:AzureOpenAI:Endpoint"],
        ["AzureOpenAI-DeploymentName"] = ["AzureOpenAI:DeploymentName", "AppSettings:AzureOpenAI:DeploymentName"],
        ["ApplicationInsights-ConnectionString"] = ["ApplicationInsights:ConnectionString"],

        // Non-shared secrets (app-specific) — prefixed with PoFunQuiz
        ["PoFunQuiz-TableStorageConnectionString"] = ["ConnectionStrings:tables", "AppSettings:Storage:TableStorageConnectionString"],
        ["PoFunQuiz-SignalRConnectionString"] = ["Azure:SignalR:ConnectionString"],
    };

    foreach (var (secretName, configPaths) in mappings)
    {
        var value = config[secretName];
        if (!string.IsNullOrEmpty(value))
        {
            foreach (var path in configPaths)
            {
                config[path] = value;
            }
            Log.Information("Mapped Key Vault secret {SecretName}", secretName);
        }
    }
}

// Make the Program class accessible to integration tests
public partial class Program { }
