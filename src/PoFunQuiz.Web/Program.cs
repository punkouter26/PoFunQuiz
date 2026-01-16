using PoFunQuiz.Web.Extensions;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Scalar.AspNetCore;
using Radzen;

// Bootstrap logger for early startup logs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting PoFunQuiz web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Aspire service defaults (OpenTelemetry, service discovery, resilience)
    builder.AddServiceDefaults();

    // Add Azure Key Vault configuration for ALL environments when endpoint is provided
    var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
    if (!string.IsNullOrWhiteSpace(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new Azure.Identity.DefaultAzureCredential());
        Log.Information("Configured Azure Key Vault: {KeyVaultEndpoint}", keyVaultEndpoint);

        // Map Key Vault secrets to configuration paths
        // Key Vault secrets use hyphens (AzureOpenAI-ApiKey), config uses colons (AzureOpenAI:ApiKey)
        var appSettings = builder.Configuration;

        // Map AzureOpenAI-ApiKey to AzureOpenAI:ApiKey
        var openAiKey = appSettings["AzureOpenAI-ApiKey"];
        if (!string.IsNullOrEmpty(openAiKey))
        {
            builder.Configuration["AzureOpenAI:ApiKey"] = openAiKey;
            builder.Configuration["AppSettings:AzureOpenAI:ApiKey"] = openAiKey;
            Log.Information("Loaded OpenAI API key from Key Vault");
        }

        // Map AzureOpenAI-Endpoint to AzureOpenAI:Endpoint
        var openAiEndpoint = appSettings["AzureOpenAI-Endpoint"];
        if (!string.IsNullOrEmpty(openAiEndpoint))
        {
            builder.Configuration["AzureOpenAI:Endpoint"] = openAiEndpoint;
            builder.Configuration["AppSettings:AzureOpenAI:Endpoint"] = openAiEndpoint;
            Log.Information("Loaded OpenAI Endpoint from Key Vault: {Endpoint}", openAiEndpoint);
        }

        // Map AzureOpenAI-DeploymentName to AzureOpenAI:DeploymentName
        var openAiDeployment = appSettings["AzureOpenAI-DeploymentName"];
        if (!string.IsNullOrEmpty(openAiDeployment))
        {
            builder.Configuration["AzureOpenAI:DeploymentName"] = openAiDeployment;
            builder.Configuration["AppSettings:AzureOpenAI:DeploymentName"] = openAiDeployment;
            Log.Information("Loaded OpenAI DeploymentName from Key Vault: {DeploymentName}", openAiDeployment);
        }

        // Map TableStorageConnectionString to AppSettings:Storage:TableStorageConnectionString
        var tableStorageConn = appSettings["TableStorageConnectionString"];
        if (!string.IsNullOrEmpty(tableStorageConn))
        {
            builder.Configuration["AppSettings:Storage:TableStorageConnectionString"] = tableStorageConn;
            Log.Information("Loaded Table Storage connection string from Key Vault");
        }

        // Map ApplicationInsights-ConnectionString to ApplicationInsights:ConnectionString
        var appInsightsConn = appSettings["ApplicationInsights-ConnectionString"];
        if (!string.IsNullOrEmpty(appInsightsConn))
        {
            builder.Configuration["ApplicationInsights:ConnectionString"] = appInsightsConn;
            Log.Information("Loaded Application Insights connection string from Key Vault");
        }
    }
    else
    {
        Log.Warning("AZURE_KEY_VAULT_ENDPOINT not configured; skipping Key Vault integration");
    }

    // Configure Serilog
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
            .Enrich.WithProperty("Application", "PoFunQuiz")
            .WriteTo.Console();

        // Add Application Insights sink if connection string is configured
        var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            configuration.WriteTo.ApplicationInsights(
                appInsightsConnectionString,
                new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
        }
    });

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();

    // Add OpenAPI
    builder.Services.AddOpenApi();

    builder.Services.AddAntiforgery();
    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();

    // Add Blazor services with Interactive Server components
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add Radzen services
    builder.Services.AddRadzenComponents();

    // Register Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<PoFunQuiz.Web.HealthChecks.TableStorageHealthCheck>("table_storage")
        .AddCheck<PoFunQuiz.Web.HealthChecks.OpenAIHealthCheck>("openai")
        .AddCheck<PoFunQuiz.Web.HealthChecks.InternetConnectivityHealthCheck>("internet");

    // Configure SignalR
    var signalRBuilder = builder.Services.AddSignalR();
    var signalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
    if (!string.IsNullOrEmpty(signalRConnectionString))
    {
        signalRBuilder.AddAzureSignalR();
        Log.Information("Configured Azure SignalR Service");
    }
    else
    {
        Log.Warning("Azure SignalR Connection String not found. Falling back to local SignalR.");
    }

    // Add Aspire Azure Tables client (connects to Azurite in development via AppHost)
    builder.AddAzureTableServiceClient("tables");

    // Register all application services (storage, business logic)
    builder.Services.AddApplicationServices(builder.Configuration);

    // Register shared state services (GameState, ConnectionState for interactive components)
    builder.Services.AddScoped<PoFunQuiz.Web.GameState>();
    builder.Services.AddScoped<PoFunQuiz.Web.ConnectionState>();

    var app = builder.Build();

    // Map Aspire default endpoints (health checks)
    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseAntiforgery();
    app.MapStaticAssets();

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapControllers();

    // Map SignalR Hub
    app.MapHub<PoFunQuiz.Web.Features.Multiplayer.GameHub>("/gamehub");

    // Map API Endpoints (Vertical Slice Architecture)
    PoFunQuiz.Web.Features.Leaderboard.GetLeaderboard.MapEndpoint(app);
    PoFunQuiz.Web.Features.Leaderboard.SubmitScore.MapEndpoint(app);

    // Map Blazor components with Interactive Server render mode
    app.MapRazorComponents<PoFunQuiz.Web.Components.App>()
        .AddInteractiveServerRenderMode();

    // Register application lifecycle events
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

// Make the Program class accessible to integration tests
public partial class Program { }
