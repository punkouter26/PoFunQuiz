# Architecture — System Context + Container Diagram (C4)

> **Full version** (C4 Level 1 + Level 2 combined)  
> Render with any Mermaid-compatible viewer (GitHub, VS Code Mermaid extension, etc.)

## Full Diagram

```mermaid
C4Context
  title PoFunQuiz — System Context (C4 Level 1)

  Person(player, "Player", "A human playing the quiz game solo or in multiplayer")
  Person(devops, "DevOps / Developer", "Deploys and monitors the application")

  System(pofunquiz, "PoFunQuiz", "Blazor Server web application — AI-generated quizzes, real-time multiplayer, leaderboard")

  System_Ext(openai, "Azure OpenAI Service", "GPT-4o — generates quiz questions on demand")
  System_Ext(signalr, "Azure SignalR Service", "Scales real-time WebSocket connections for multiplayer")
  System_Ext(tables, "Azure Table Storage", "Persists leaderboard scores (PoFunQuizPlayers table)")
  System_Ext(keyvault, "Azure Key Vault (PoShared)", "Stores all secrets: API keys, connection strings")
  System_Ext(appinsights, "Application Insights", "Telemetry, traces, metrics, logs aggregation")
  System_Ext(azuread, "Azure Managed Identity", "Keyless auth to KeyVault + Storage (no stored credentials)")

  Rel(player, pofunquiz, "Plays quiz, views leaderboard", "HTTPS / WebSocket")
  Rel(devops, pofunquiz, "Deploys via azd, monitors /health", "azd CLI / Azure Portal")
  Rel(pofunquiz, openai, "Generates questions", "HTTPS REST")
  Rel(pofunquiz, signalr, "Multiplayer sync", "WebSocket")
  Rel(pofunquiz, tables, "Read/write scores", "HTTPS SDK")
  Rel(pofunquiz, keyvault, "Fetch secrets at startup", "HTTPS / Managed Identity")
  Rel(pofunquiz, appinsights, "Emit traces + metrics", "OTLP / SDK")
  Rel(pofunquiz, azuread, "Authenticate to Azure", "MSI token")
```

## Container Diagram (C4 Level 2)

```mermaid
C4Container
  title PoFunQuiz — Container Diagram (C4 Level 2)

  Person(player, "Player")

  Container_Boundary(azure, "Azure — rg-PoFunQuiz") {
    Container(web, "PoFunQuiz.Web", ".NET 10 Blazor Server", "Serves UI + API. Handles quiz, multiplayer, leaderboard.")
    ContainerDb(tablestore, "Azure Table Storage", "NoSQL Table", "Stores LeaderboardEntry rows in PoFunQuizPlayers table")
    Container(signalrhub, "GameHub (SignalR)", "ASP.NET Core Hub", "Real-time multiplayer events: PlayerJoined, GameStarted, ScoreUpdated")
  }

  Container_Boundary(shared, "rg-PoShared (Shared Resources)") {
    Container(kv, "Key Vault kv-poshared", "Azure Key Vault", "Secrets: OpenAI key/endpoint, SignalR conn str, App Insights conn str")
    Container(law, "Log Analytics law-poshared", "Azure Monitor", "Central log sink + dashboard")
    Container(mi, "Managed Identity mi-poshared-apps", "User-Assigned MI", "Grants keyless access to KeyVault + Storage")
  }

  System_Ext(openai, "Azure OpenAI gpt-4o")

  Rel(player, web, "HTTP/HTTPS", "port 443")
  Rel(web, signalrhub, "SignalR WebSocket", "wss://")
  Rel(web, tablestore, "Azure.Data.Tables SDK", "HTTPS")
  Rel(web, kv, "Reads secrets on startup", "Managed Identity")
  Rel(web, openai, "REST POST /chat/completions", "HTTPS")
  Rel(web, law, "OpenTelemetry + Serilog", "OTLP")
  Rel(web, mi, "Uses for auth", "MSI")
```

---

## Simplified Diagram

```mermaid
graph TB
    subgraph Client
        B[Browser - Blazor WASM/SSR]
    end

    subgraph Azure App Service
        W[PoFunQuiz.Web<br/>.NET 10 Blazor Server]
        HUB[GameHub<br/>SignalR]
    end

    subgraph Azure Services
        OAI[Azure OpenAI<br/>GPT-4o]
        TS[Table Storage<br/>Leaderboard]
        KV[Key Vault<br/>PoShared]
        AI[Application Insights]
        SR[Azure SignalR Service]
    end

    B <-->|HTTP + WebSocket| W
    W --- HUB
    W -->|Generate questions| OAI
    W -->|Read/Write scores| TS
    W -->|Fetch secrets| KV
    W -->|Telemetry| AI
    HUB <-->|Scale WebSockets| SR
```
