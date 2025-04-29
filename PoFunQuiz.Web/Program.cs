using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Infrastructure.Services;
using PoFunQuiz.Web.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Azure.Data.Tables; // Add using for TableServiceClient
using Microsoft.Extensions.Configuration; // Add for GetValue
using Serilog;
using Serilog.Events;

// Create a new log file for each run
var logFileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(logFileName, 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting PoFunQuiz web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(options => {
            // Add more resilient SignalR configuration
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            options.DisconnectedCircuitMaxRetained = 100;
            options.DetailedErrors = true;
        });

    // Configure SignalR for more robust connections
    builder.Services.AddServerSideBlazor()
        .AddHubOptions(options => {
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        });

    // Add a custom CircuitHandler to detect connection state changes
    builder.Services.AddSingleton<CircuitHandler, PoFunQuiz.Web.Services.ConnectionStateHandler>();

    // Add MudBlazor services
    builder.Services.AddMudServices();

    // Configure services
    builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
    builder.Services.Configure<TableStorageSettings>(builder.Configuration.GetSection("AzureTableStorage"));

    // Register TableServiceClient as a singleton
    builder.Services.AddSingleton(sp => 
    {
        // Use IConfiguration directly, available via builder.Configuration
        var connectionString = builder.Configuration.GetValue<string>("AzureTableStorage:ConnectionString"); 
        if (string.IsNullOrEmpty(connectionString))
        {
            // Log this error as well - requires ILogger registration or access if needed here
            // For simplicity, just throwing exception now. Add logging if necessary.
            // var logger = sp.GetRequiredService<ILogger<Program>>(); 
            // logger.LogError("Azure Table Storage connection string ('AzureTableStorage:ConnectionString') not found or empty in configuration.");
            throw new InvalidOperationException("Azure Table Storage connection string ('AzureTableStorage:ConnectionString') not found or empty in configuration.");
        }
        return new TableServiceClient(connectionString);
    });

    // Register services
    builder.Services.AddScoped<IQuestionGeneratorService, OpenAIQuestionGeneratorService>();
    builder.Services.AddScoped<IPlayerStorageService, PlayerStorageService>();
    builder.Services.AddScoped<IGameSessionService, GameSessionService>();
    builder.Services.AddSingleton<PoFunQuiz.Web.Services.GameState>();
    builder.Services.AddSingleton<PoFunQuiz.Web.Services.ConnectionState>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<PoFunQuiz.Web.Components.App>()
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
