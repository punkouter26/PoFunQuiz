# ProjectManifest — Asset Inventory

> This file is the AI-agent entry point. It maps every documentation asset, code artifact, and key file so agents can navigate the project without full filesystem discovery.

---

## Documentation Assets (`/docs`)

| File | Purpose | Audience | Diagrams Included |
|------|---------|----------|-------------------|
| [Architecture.mmd.md](Architecture.mmd.md) | System Context (C4 L1) + Container (C4 L2) | All | C4Context, C4Container, simplified flowchart |
| [ApplicationFlow.mmd.md](ApplicationFlow.mmd.md) | Auth flow, Solo journey, Multiplayer journey, Page nav | Dev + QA | 4× sequence/state diagrams |
| [DataModel.mmd.md](DataModel.mmd.md) | ERD, scoring formula, state machine, Table Storage schema | Dev + DB | ERD, graph, stateDiagram, classDiagram |
| [ComponentMap.mmd.md](ComponentMap.mmd.md) | Blazor component tree, service DI, feature slice map | Dev | 3× graph diagrams |
| [DataPipeline.mmd.md](DataPipeline.mmd.md) | Question generation, score submission, leaderboard read, SignalR sync | Dev | 4× flowchart + sequence |
| [ProductSpec.md](ProductSpec.md) | PRD, features, personas, scoring model, success metrics | PM + Dev | Tables only |
| [ApiContract.md](ApiContract.md) | All REST endpoints, SignalR hub methods, error policy | Dev + QA | Inline JSON examples |
| [DevOps.md](DevOps.md) | azd deployment, Bicep infra, secrets, CI/CD pipeline | DevOps | 1× flowchart |
| [LocalSetup.md](LocalSetup.md) | Day 1 guide, Docker Compose, secrets, test commands | Dev | Docker reference |
| [ProjectManifest.md](ProjectManifest.md) | This file — AI-parseable asset map | AI agents | None |

---

## Source Code Map (`/src/PoFunQuiz.Web`)

### Entry Points

| File | Purpose |
|------|---------|
| `Program.cs` | App bootstrap, middleware, DI container, endpoint registration |
| `PoFunQuiz.Web.csproj` | NuGet dependencies (Central Package Management) |
| `appsettings.json` | Default config + feature flags |
| `appsettings.Development.json` | Dev overrides |
| `GameState.cs` | Scoped Blazor service — active game session state |

### Features (Vertical Slice Architecture)

| Slice | Files | API/Hub Endpoints |
|-------|-------|-------------------|
| Quiz | `Features/Quiz/QuizEndpoints.cs`, `OpenAIService.cs`, `QuizQuestionDeserializers.cs` | `GET /api/quiz/questions` |
| Leaderboard | `Features/Leaderboard/GetLeaderboard.cs`, `SubmitScore.cs`, `LeaderboardRepository.cs`, `ILeaderboardRepository.cs` | `GET /api/leaderboard`, `POST /api/leaderboard` |
| Multiplayer | `Features/Multiplayer/GameHub.cs`, `MultiplayerLobbyService.cs`, `GameClientService.cs`, `SessionReaperService.cs`, `MultiplayerDtos.cs` | SignalR `/gamehub` |
| Storage | `Features/Storage/TableStorageInitializer.cs` | Background initialization |

### Models

| Model | Key Fields |
|-------|-----------|
| `Models/GameSession.cs` | Full game state, computed score totals, Winner/IsTie |
| `Models/QuizQuestion.cs` | Question text, Options[4], CorrectOptionIndex, Difficulty, BasePoints |
| `Models/LeaderboardEntry.cs` | ITableEntity implementation, Score, MaxStreak, Wins/Losses |
| `Models/Player.cs` | Name, Initials |

### Components (Blazor)

| Component | Route | Description |
|-----------|-------|-------------|
| `Pages/Home.razor` | `/` | Landing page + navigation |
| `Pages/GameSetup.razor` | `/game-setup` | Category + difficulty selection |
| `Pages/GameBoard.razor` | `/game` | Active quiz with timer + scoring |
| `Pages/Results.razor` | `/results` | Score breakdown + submit |
| `Pages/Leaderboard.razor` | `/leaderboard` | Top-10 scores per category |
| `Pages/MultiplayerLobby.razor` | `/multiplayer` | Create/join game sessions |
| `Pages/Diag.razor` | `/diag` | Config diagnostics |
| `Game/GameTimer.razor` | - | Countdown timer component |
| `Game/PlayerBoard.razor` | - | Per-player Q&A display |
| `Game/ScoreBoard.razor` | - | Live score display |

---

## Infrastructure Map (`/infra`)

| File | Purpose |
|------|---------|
| `main.bicep` | Subscription-scoped entry; creates `PoFunQuiz` resource group |
| `resources.bicep` | App Service Plan, App Service, references to PoShared |
| `storage/storage.module.bicep` | Storage Account with Table support |
| `storage-roles/storage-roles.module.bicep` | RBAC: Managed Identity → Storage Table Data Contributor |
| `main.parameters.json` | `environmentName`, `location`, `principalId` |

---

## Tests Map (`/tests`)

| Project | Type | Key Files |
|---------|------|-----------|
| `PoFunQuiz.Tests` | Unit + Integration | `OpenAIServiceTests.cs`, `QuestionConsistencyTests.cs` |
| `PoFunQuiz.Tests/Unit/` | Pure unit tests | `GameStateTests.cs` (active file) |
| `PoFunQuiz.Tests/Integration/` | API + Hub tests | `GameHubTests.cs`, `LeaderboardApiTests.cs`, `HealthCheckTests.cs`, `QuizControllerTests.cs` |
| `PoFunQuiz.Tests/Services/` | Service-layer tests | OpenAI, storage services |
| `PoFunQuiz.E2ETests` | Playwright TypeScript E2E | `game-flow.spec.ts`, `multiplayer.spec.ts`, `homepage-startup.spec.ts` |

---

## Key Configuration Keys

| Key | Source | Used By |
|-----|--------|---------|
| `AzureOpenAI:ApiKey` | Key Vault / user-secrets | OpenAIService |
| `AzureOpenAI:Endpoint` | Key Vault / user-secrets | OpenAIService |
| `AzureOpenAI:DeploymentName` | appsettings / Key Vault | OpenAIService (default: `gpt-4o`) |
| `ConnectionStrings:tables` | Key Vault / user-secrets / Bicep | TableServiceClient |
| `Azure:SignalR:ConnectionString` | Key Vault | SignalR service (optional) |
| `ApplicationInsights:ConnectionString` | Key Vault | Azure Monitor SDK |
| `AZURE_KEY_VAULT_ENDPOINT` | App Service env var / none locally | Key Vault bootstrap |
| `AppSettings:FeatureFlags:*` | appsettings.json | Feature toggles |

---

## Quick Reference — Ports & URLs

| Service | Local URL | Production URL |
|---------|-----------|----------------|
| App (HTTP) | http://localhost:5000 | https://app-pofunquiz-{token}.azurewebsites.net |
| App (HTTPS) | https://localhost:5001 | same |
| Health check | /health | /health |
| Diagnostics | /diag | /diag |
| API docs | /scalar/v1 | /scalar/v1 |
| SignalR hub | /gamehub | /gamehub |
| Azurite Table | http://127.0.0.1:10002 | N/A |
