using PoFunQuiz.Server.Extensions;
using PoFunQuiz.Server.Middleware;
using Serilog;
using System.IO;

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
    builder.Services.AddApplicationInsightsTelemetry();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddAntiforgery();
    builder.Services.AddAuthorization(); // Add this line
    builder.Services.AddControllers(); // Add this line

    // Register all application services
    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // Add global exception handler middleware
    app.UseGlobalExceptionHandler();

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();
    app.UseAuthorization(); // Add UseAuthorization for API

    app.MapControllers(); // Map controllers for API
    app.MapFallbackToFile("index.html");

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
