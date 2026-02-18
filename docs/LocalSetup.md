# LocalSetup — Day 1 Guide + Docker Compose

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org (for E2E tests) |
| Docker Desktop | Latest | https://docker.com/products/docker-desktop |
| Azure Developer CLI | Latest | `winget install Microsoft.Azd` |
| Git | Latest | https://git-scm.com |

Optional but recommended:
- VS Code with C# Dev Kit extension
- Mermaid Preview extension (to view `.mmd.md` diagrams)

---

## Step 1 — Clone the Repository

```bash
git clone https://github.com/punkouter/PoFunQuiz.git
cd PoFunQuiz
```

---

## Step 2 — Start Local Azure Storage Emulator

The app requires Azure Table Storage. For local dev, use **Azurite** via Docker:

```bash
docker-compose up -d
```

This starts the Azurite emulator with Table Storage on port `10002`.  
Verify: `http://127.0.0.1:10002/devstoreaccount1`

**`docker-compose.yml` services:**
- `azurite` — Azure Storage emulator (Blob: 10000, Queue: 10001, Table: 10002)

---

## Step 3 — Configure User Secrets

The app uses `dotnet user-secrets` for local secrets. At minimum, set your Azure OpenAI credentials:

```bash
cd src/PoFunQuiz.Web

# Required: Azure OpenAI
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-azure-openai-api-key>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"

# Optional: Use Azurite (already the default in appsettings.Development.json)
dotnet user-secrets set "ConnectionStrings:tables" "UseDevelopmentStorage=true"
```

**No Azure account?** The app falls back to generating placeholder questions if OpenAI is unconfigured.

---

## Step 4 — Run the Application

```bash
# From repo root
dotnet run --project src/PoFunQuiz.Web/PoFunQuiz.Web.csproj
```

The app starts at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

You should see logs like:
```
[10:00:00 INF] Starting PoFunQuiz web application
[10:00:01 INF] Configured Azure Key Vault: (or warning if not set)
[10:00:02 INF] Now listening on: http://localhost:5000
[10:00:02 INF] Application started
```

---

## Step 5 — Verify Health

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "openai", "status": "Healthy" },
    { "name": "table-storage", "status": "Healthy" }
  ]
}
```

Check diagnostics:
```bash
curl http://localhost:5000/diag
```

---

## Step 6 — Explore the API

Open the Scalar API UI at: `http://localhost:5000/scalar/v1`

Or use the `.http` file in VS Code:
```
src/PoFunQuiz.Web/PoFunQuiz.http
```

Sample requests:
```http
### Get 5 Science questions
GET http://localhost:5000/api/quiz/questions?count=5&category=Science

### Get leaderboard
GET http://localhost:5000/api/leaderboard?category=General

### Submit a score
POST http://localhost:5000/api/leaderboard
Content-Type: application/json

{
  "playerName": "Alice",
  "score": 850,
  "maxStreak": 4,
  "category": "Science",
  "wins": 1,
  "losses": 0
}
```

---

## Step 7 — Run Tests

### Unit + Integration Tests

```bash
dotnet test tests/PoFunQuiz.Tests/PoFunQuiz.Tests.csproj
```

Integration tests use Testcontainers for isolated DB testing (Docker required).

### E2E Tests (Playwright)

```bash
cd tests/PoFunQuiz.E2ETests

# Install dependencies
npm install

# Install Playwright browsers (first time only)
npx playwright install chromium

# Run E2E tests (app must be running on localhost:5000)
npx playwright test --project=chromium

# Run headed (visible browser)
npx playwright test --project=chromium --headed
```

Test files:
- `homepage-startup.spec.ts` — app loads, nav works
- `game-start.spec.ts` — solo game flow
- `game-flow.spec.ts` — full question/answer loop
- `multiplayer.spec.ts` — 2-player session
- `ui-navigation.spec.ts` — page navigation

---

## Docker Compose Reference

```yaml
# docker-compose.yml
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    container_name: pofunquiz-azurite
    ports:
      - "10000:10000"  # Blob
      - "10001:10001"  # Queue
      - "10002:10002"  # Table
    volumes:
      - azurite-data:/data

volumes:
  azurite-data:
```

### Useful Docker Commands

```bash
# Start emulator
docker-compose up -d

# Stop emulator
docker-compose down

# View logs
docker-compose logs -f azurite

# Reset storage data
docker-compose down -v && docker-compose up -d
```

---

## Project Ports Reference

| Service | Port | Protocol |
|---------|------|---------|
| PoFunQuiz.Web (HTTP) | 5000 | HTTP |
| PoFunQuiz.Web (HTTPS) | 5001 | HTTPS |
| Azurite Blob | 10000 | HTTP |
| Azurite Queue | 10001 | HTTP |
| Azurite Table | 10002 | HTTP |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `UseDevelopmentStorage=true` connection fails | Ensure `docker-compose up -d` has been run |
| OpenAI errors | Verify `AzureOpenAI:ApiKey` and `Endpoint` secrets are set |
| HTTPS cert error | Run `dotnet dev-certs https --trust` |
| Port 5000 in use | `netstat -ano \| findstr 5000` then kill the process |
| Playwright tests fail | Ensure app is running; check `playwright.config.ts` for `baseURL` |
| `Already full` when joining multiplayer | Max 2 players; use a new game code |

---

## Feature Flags

Configured in `appsettings.json` under `AppSettings.FeatureFlags`:

| Flag | Default | Description |
|------|---------|-------------|
| `EnableNewLeaderboard` | `true` | New leaderboard UI |
| `EnableExperimentalGameMode` | `true` | Experimental features |
| `EnableBrowserLoggingIntegration` | `true` | Browser console logging |
| `EnableDiagPage` | `true` | `/diag` endpoint active |
