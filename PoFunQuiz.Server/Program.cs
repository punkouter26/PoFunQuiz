using PoFunQuiz.Server.Extensions;
using PoFunQuiz.Server.Middleware;
using PoFunQuiz.Server.HealthChecks;
using Serilog;
using System.IO;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Scalar.AspNetCore;

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

    // Register OpenAI Service
    builder.Services.AddScoped<PoFunQuiz.Server.Services.IOpenAIService, PoFunQuiz.Server.Services.OpenAIService>();

    // Register all application services
    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

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

    app.UseHttpsRedirection();

    // Add frontend selector middleware before static files
    app.UseMiddleware<FrontendSelectorMiddleware>();

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

    // Blazor fallback configuration
    app.MapFallbackToFile("index.html");

    // Register and log application lifecycle events
    var lifetime = app.Lifetime;
    lifetime.ApplicationStarted.Register(() => Log.Information("Application started"));
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
