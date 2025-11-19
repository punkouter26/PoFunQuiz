using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoFunQuiz.Client;
using PoFunQuiz.Client.Services;
using PoFunQuiz.Core.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to communicate with the server
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register Game Client Service
builder.Services.AddScoped<GameClientService>();

// Add Radzen services
builder.Services.AddRadzenComponents();

// Register the client-side question generator service
builder.Services.AddScoped<IQuestionGeneratorService, ClientQuestionGeneratorService>();

// Add application state services
builder.Services.AddSingleton<GameState>();
builder.Services.AddSingleton<ConnectionState>();

await builder.Build().RunAsync();
