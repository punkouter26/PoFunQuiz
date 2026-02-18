# PoFunQuiz

**AI-powered, real-time multiplayer quiz game** built on **.NET 10 Blazor Server**, deployed to **Azure App Service**.

Players compete in solo or head-to-head trivia challenges across 8 categories. Questions are generated on-demand by **Azure OpenAI GPT-4o** — no static question bank. Scores are persisted to a global leaderboard in **Azure Table Storage**. Real-time multiplayer is powered by **SignalR** (with optional Azure SignalR Service for scale-out).

---

## Quick Start

```bash
# 1. Start local Azure Storage emulator
docker-compose up -d

# 2. Set secrets
cd src/PoFunQuiz.Web
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"

# 3. Run
cd ../..
dotnet run --project src/PoFunQuiz.Web/PoFunQuiz.Web.csproj
```

App runs at `http://localhost:5000` · API docs at `http://localhost:5000/scalar/v1`

---

## Architecture Overview

```
Browser (Blazor Server)
    │
    ├── HTTPS ──────────────► Azure App Service (PoFunQuiz.Web / .NET 10)
    │                              │
    │                              ├── Azure OpenAI GPT-4o  (question generation)
    │                              ├── Azure Table Storage   (leaderboard)
    │                              ├── Azure SignalR Service (multiplayer WebSocket)
    │                              └── Azure Key Vault PoShared (secrets via Managed Identity)
    │
    └── WebSocket ──────────► SignalR GameHub (/gamehub)
```

Key design choices:
- **Vertical Slice Architecture** — features (Quiz, Leaderboard, Multiplayer) are self-contained
- **No authentication** — anonymous play, PlayerName is self-reported
- **In-memory multiplayer sessions** — `MultiplayerLobbyService` (singleton), `SessionReaperService` handles TTL
- **Output caching** — quiz question responses cached 60 s by `count + category` to reduce OpenAI API spend
- **Zero stored credentials** — all secrets via Azure Key Vault + Managed Identity

---

## Features

| Feature | Description |
|---------|-------------|
| Solo Quiz | 10 AI-generated questions per session, timed, with streak/speed/time bonuses |
| Multiplayer | 2-player real-time competition via SignalR, host-only start authority |
| Leaderboard | Per-category top-10, persisted to Azure Table Storage |
| 8 Categories | General, Science, History, Geography, Technology, Sports, Entertainment, Arts |
| 3 Difficulty Levels | Easy (1pt), Medium (2pt), Hard (3pt) per question |
| `/health` | JSON health check for OpenAI + Table Storage |
| `/diag` | Masked config dump for debugging |
| `/scalar/v1` | Interactive OpenAPI documentation |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor Server (.NET 10), Radzen UI components |
| Backend | ASP.NET Core Minimal APIs, SignalR |
| AI | Azure OpenAI GPT-4o |
| Storage | Azure Table Storage (Azure.Data.Tables SDK) |
| Real-time | SignalR / Azure SignalR Service |
| Secrets | Azure Key Vault + Managed Identity (DefaultAzureCredential) |
| Observability | Serilog + OpenTelemetry → Application Insights |
| IaC | Azure Bicep + Azure Developer CLI (azd) |
| Testing | xUnit + Testcontainers (integration), Playwright (E2E) |

---

## Project Structure

```
PoFunQuiz/
├── src/PoFunQuiz.Web/          # Main Blazor application
│   ├── Features/               # Vertical slices: Quiz, Leaderboard, Multiplayer, Storage
│   ├── Components/Pages/       # Blazor pages: Home, GameBoard, Results, Leaderboard, etc.
│   ├── Models/                 # GameSession, QuizQuestion, LeaderboardEntry, Player
│   ├── HealthChecks/           # OpenAI + TableStorage health checks
│   └── Program.cs              # Bootstrap, DI, middleware, endpoints
├── tests/
│   ├── PoFunQuiz.Tests/        # Unit + Integration tests (xUnit + Testcontainers)
│   └── PoFunQuiz.E2ETests/     # Playwright TypeScript E2E tests
├── infra/                      # Bicep IaC (App Service, Storage, RBAC)
├── docker-compose.yml          # Azurite local storage emulator
└── azure.yaml                  # azd configuration
```

---

## Documentation

Full documentation is in the [`/docs`](docs/) folder:

| Document | Description |
|----------|-------------|
| [Architecture.mmd.md](docs/Architecture.mmd.md) | System Context + Container Architecture (C4) |
| [ApplicationFlow.mmd.md](docs/ApplicationFlow.mmd.md) | Auth Flow + User Journeys (Solo + Multiplayer) |
| [DataModel.mmd.md](docs/DataModel.mmd.md) | Database Schema + State Transitions |
| [ComponentMap.mmd.md](docs/ComponentMap.mmd.md) | Blazor Component Tree + Service Dependencies |
| [DataPipeline.mmd.md](docs/DataPipeline.mmd.md) | Data Workflows (CRUD + Process Flows) |
| [ProductSpec.md](docs/ProductSpec.md) | PRD + Success Metrics + Feature Descriptions |
| [ApiContract.md](docs/ApiContract.md) | REST + SignalR API Specs + Error Handling Policy |
| [DevOps.md](docs/DevOps.md) | Deployment Pipeline + Environment Secrets |
| [LocalSetup.md](docs/LocalSetup.md) | Day 1 Developer Guide + Docker Compose |
| [ProjectManifest.md](docs/ProjectManifest.md) | Asset Inventory (AI agent entry point) |
| [Suggestions.md](docs/Suggestions.md) | Top 5 Documentation Improvement Suggestions |

---

## Deployment

```bash
# Install Azure Developer CLI
winget install Microsoft.Azd

# Login + provision + deploy
azd auth login
azd up
```

Deploys to Azure App Service (B1 Linux) in `rg-PoFunQuiz` resource group.  
Subscription: `Punkouter26` (`bbb8dfce-9169-432f-9b7a-fbf861b51037`)

See [docs/DevOps.md](docs/DevOps.md) for full deployment guide.

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/quiz/questions?count=10&category=Science` | Generate AI quiz questions |
| GET | `/api/leaderboard?category=General` | Top-10 scores per category |
| POST | `/api/leaderboard` | Submit a player score |
| GET | `/health` | Health check (JSON) |
| GET | `/diag` | Diagnostic config dump (masked) |

SignalR hub: `/gamehub` — see [docs/ApiContract.md](docs/ApiContract.md)

---

## Running Tests

```bash
# Unit + Integration
dotnet test tests/PoFunQuiz.Tests/

# E2E (app must be running)
cd tests/PoFunQuiz.E2ETests
npm install && npx playwright install chromium
npx playwright test --project=chromium
```

---

## Naming Convention

Follows `Po{SolutionName}` prefix convention across namespaces, resource groups, and Azure resources:
- Namespace: `PoFunQuiz.Web.*`
- Resource Group: `PoFunQuiz`
- Shared Resources: `PoShared`
