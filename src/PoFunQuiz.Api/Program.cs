using PoFunQuiz.Server.Extensions;
using PoFunQuiz.Server.Middleware;
using PoFunQuiz.Server.HealthChecks;
using Serilog;
using System.IO;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Scalar.AspNetCore;
using Microsoft.Azure.SignalR;

// Bootstrap logger so early startup logs are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting PoFunQuiz web application");

    var builder = WebApplication.CreateBuilder(args);

    // Load secrets.json for local development
    var secretsPath = Path.Combine(Directory.GetCurrentDirectory(), "secrets.json");
    if (File.Exists(secretsPath))
    {
        builder.Configuration.AddJsonFile(secretsPath, optional: true, reloadOnChange: true);
        Log.Information("Loaded secrets.json configuration file");
    }
    else
    {
        Log.Warning("secrets.json file not found");
    }

    // Add Azure Key Vault configuration for all environments when endpoint is provided
    var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
    if (!string.IsNullOrWhiteSpace(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new Azure.Identity.DefaultAzureCredential());
        Log.Information("Configured Azure Key Vault: {KeyVaultEndpoint}", keyVaultEndpoint);
        
        // Override configuration with Key Vault secrets
        // Map Key Vault secrets to configuration paths
        var appSettings = builder.Configuration;
        
        // Map AzureOpenAI--ApiKey to AzureOpenAI:ApiKey
        var openAiKey = appSettings["AzureOpenAI--ApiKey"];
        if (!string.IsNullOrEmpty(openAiKey))
        {
            builder.Configuration["AzureOpenAI:ApiKey"] = openAiKey;
            Log.Information("Loaded OpenAI API key from Key Vault");
        }
        
        // Map TableStorageConnectionString to AppSettings:Storage:TableStorageConnectionString
        var tableStorageConn = appSettings["TableStorageConnectionString"];
        if (!string.IsNullOrEmpty(tableStorageConn))
        {
            builder.Configuration["AppSettings:Storage:TableStorageConnectionString"] = tableStorageConn;
            Log.Information("Loaded Table Storage connection string from Key Vault");
        }
        
        // Map ApplicationInsights--ConnectionString to ApplicationInsights:ConnectionString
        var appInsightsConn = appSettings["ApplicationInsights--ConnectionString"];
        if (!string.IsNullOrEmpty(appInsightsConn))
        {
            builder.Configuration["ApplicationInsights:ConnectionString"] = appInsightsConn;
            Log.Information("Loaded Application Insights connection string from Key Vault");
        }
    }
    else
    {
        Log.Warning("AZURE_KEY_VAULT_ENDPOINT not configured; skipping Key Vault integration");
    }    // Configure logging
    builder.Host.AddApplicationLogging();

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddAntiforgery();
    builder.Services.AddAuthorization(); // Add this line
    builder.Services.AddControllers(); // Add this line
    builder.Services.AddHttpClient(); // Add HttpClient factory

    // Register Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<TableStorageHealthCheck>("table_storage")
        .AddCheck<OpenAIHealthCheck>("openai")
        .AddCheck<InternetConnectivityHealthCheck>("internet");

    var signalRBuilder = builder.Services.AddSignalR();
    
    // Only add Azure SignalR if a connection string is configured
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

    // Register all application services
    builder.Services.AddApplicationServices(builder.Configuration);    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Enable Swagger in all environments for API testing (Phase 5 requirement)
    app.MapOpenApi();
    app.MapScalarApiReference();  // Modern Swagger UI at /scalar/v1

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // Add global exception handler middleware
    app.UseGlobalExceptionHandler();

    // Only use HTTPS redirection in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseAuthorization(); // Add UseAuthorization for API

    // Map health check endpoint
    app.MapHealthChecks("/api/health", new HealthCheckOptions
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

    app.MapControllers(); // Map controllers for API

    // Map SignalR Hub
    app.MapHub<PoFunQuiz.Api.Features.Multiplayer.GameHub>("/gamehub");

    // Map Leaderboard Endpoints
    PoFunQuiz.Api.Features.Leaderboard.GetLeaderboard.MapEndpoint(app);
    PoFunQuiz.Api.Features.Leaderboard.SubmitScore.MapEndpoint(app);

    // Blazor fallback configuration
    app.MapFallbackToFile("index.html");

    // Register and log application lifecycle events
    var lifetime = app.Lifetime;
    lifetime.ApplicationStarted.Register(() => 
    {
        var addresses = app.Services.GetService<Microsoft.AspNetCore.Hosting.Server.IServer>()?.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;
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
