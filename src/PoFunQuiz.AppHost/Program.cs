var builder = DistributedApplication.CreateBuilder(args);

// Azure Key Vault endpoint for secrets management (from PoShared resource group)
var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"] 
    ?? "https://kv-poshared.vault.azure.net/";

// Azure Table Storage for leaderboard data
// In development, uses Azurite emulator; in production, uses Azure Storage
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));

var tableStorage = storage.AddTables("tables");

// Main Web application (Blazor Web App with SSR + WASM)
var web = builder.AddProject<Projects.PoFunQuiz_Web>("pofunquiz-web")
    .WithExternalHttpEndpoints()
    .WithReference(tableStorage)
    .WaitFor(tableStorage)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AZURE_KEY_VAULT_ENDPOINT", keyVaultEndpoint);

builder.Build().Run();
