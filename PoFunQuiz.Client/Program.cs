using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoFunQuiz.Client;
using PoFunQuiz.Client.Services; // Add this using statement
using PoFunQuiz.Core.Services; // Add this using statement
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to communicate with the server
// Dynamically set API base URL based on environment
var baseAddress = builder.HostEnvironment.IsDevelopment()
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri("https://pofunquiz.azurewebsites.net/");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = baseAddress });

// Add Radzen services
builder.Services.AddRadzenComponents();

// Register the client-side question generator service
builder.Services.AddScoped<IQuestionGeneratorService, ClientQuestionGeneratorService>();

// Register client-side logging service
builder.Services.AddScoped<IClientLogger, ClientLogger>();

// Add application state services
builder.Services.AddSingleton<GameState>();
builder.Services.AddSingleton<ConnectionState>();

await builder.Build().RunAsync();
