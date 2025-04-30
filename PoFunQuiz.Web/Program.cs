using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoFunQuiz.Web.Components;
using PoFunQuiz.Web.Extensions;
using PoFunQuiz.Web.Middleware;
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
    }

    // Configure logging
    builder.Host.AddApplicationLogging();

    // Register all application services
    builder.Services.AddApplicationServices(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    // Add global exception handler middleware
    app.UseGlobalExceptionHandler();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

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
