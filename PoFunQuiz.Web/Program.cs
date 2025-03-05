using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using PoFunQuiz.Core.Services;
using PoFunQuiz.Core.Configuration;
using PoFunQuiz.Infrastructure.Services;
using PoFunQuiz.Web.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

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
