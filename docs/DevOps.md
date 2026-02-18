# DevOps — Deployment Pipeline + Environment Secrets

## Deployment Overview

**Tool:** Azure Developer CLI (`azd`)  
**IaC:** Bicep (`infra/`)  
**Target:** Azure App Service (Linux, .NET 10)  
**Subscription:** `Punkouter26` (`bbb8dfbe-9169-432f-9b7a-fbf861b51037`)  
**Resource Group:** `PoFunQuiz`  
**Shared Resources:** `PoShared` resource group

---

## Azure Infrastructure

```
infra/
├── main.bicep                    # Subscription-scoped entry; creates rg-PoFunQuiz
├── main.parameters.json          # Environment parameters (environmentName, location)
├── resources.bicep               # App Service Plan + App Service + references to PoShared
├── storage/
│   └── storage.module.bicep      # Azure Storage Account
└── storage-roles/
    └── storage-roles.module.bicep # RBAC role assignments for Managed Identity → Storage
```

### Provisioned Resources

| Resource | Name Pattern | Tier | Purpose |
|----------|-------------|------|---------|
| Resource Group | `PoFunQuiz` | - | Container for app resources |
| App Service Plan | `asp-pofunquiz-{token}` | B1 Linux | Hosts Blazor app |
| App Service | `app-pofunquiz-{token}` | - | Runs .NET 10 container |
| Storage Account | auto-named | LRS | Table Storage for leaderboard |

### Shared Resources (rg-PoShared)

| Resource | Name | Purpose |
|----------|------|---------|
| Managed Identity | `mi-poshared-apps` | Keyless auth to Key Vault + Storage |
| Key Vault | `kv-poshared` | Stores all app secrets |
| Log Analytics | `law-poshared` | Central log aggregation |

---

## Deployment Commands

### First-time Setup

```bash
# 1. Install Azure Developer CLI
winget install Microsoft.Azd

# 2. Login
azd auth login

# 3. Initialize environment
azd env new pofunquiz-prod

# 4. Set subscription (Punkouter26)
azd env set AZURE_SUBSCRIPTION_ID bbb8dfbe-9169-432f-9b7a-fbf861b51037

# 5. Set location
azd env set AZURE_LOCATION eastus

# 6. Provision infrastructure
azd provision

# 7. Deploy application
azd deploy
```

### Subsequent Deployments

```bash
# Provision + Deploy in one command
azd up

# Deploy only (no infra changes)
azd deploy
```

### Teardown

```bash
azd down --force --purge
```

---

## Secrets Management

### Required Secrets in Key Vault (`kv-poshared`)

| Secret Name | Maps To Config Key | Description |
|-------------|-------------------|-------------|
| `AzureOpenAI-ApiKey` | `AzureOpenAI:ApiKey` | Azure OpenAI API key |
| `AzureOpenAI-Endpoint` | `AzureOpenAI:Endpoint` | e.g. `https://xxx.openai.azure.com/` |
| `AzureOpenAI-DeploymentName` | `AzureOpenAI:DeploymentName` | e.g. `gpt-4o` |
| `ApplicationInsights-ConnectionString` | `ApplicationInsights:ConnectionString` | App Insights telemetry |
| `PoFunQuiz-TableStorageConnectionString` | `ConnectionStrings:tables` | Table storage (or use Managed Identity endpoint) |
| `PoFunQuiz-SignalRConnectionString` | `Azure:SignalR:ConnectionString` | Optional: Azure SignalR Service |

### Local Development Secrets

Use `dotnet user-secrets` (project UserSecretsId: `5f56cf6a-47e1-48fe-bc26-feadc3f8b58b`):

```bash
cd src/PoFunQuiz.Web

dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"
dotnet user-secrets set "ConnectionStrings:tables" "UseDevelopmentStorage=true"
```

---

## App Service Environment Variables (set by Bicep)

| Variable | Value | Purpose |
|----------|-------|---------|
| `AZURE_KEY_VAULT_ENDPOINT` | `https://kv-poshared.vault.azure.net/` | Key Vault URL |
| `AZURE_CLIENT_ID` | Managed Identity clientId | Allows DefaultAzureCredential to use MI |
| `ConnectionStrings__tables` | Storage Table endpoint | Table Storage URL |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | KV reference | App Insights (via @Microsoft.KeyVault reference) |

---

## CI/CD Pipeline (GitHub Actions)

> Add `.github/workflows/azure-dev.yml` to automate on push to `main`.

```yaml
# .github/workflows/azure-dev.yml
name: Deploy PoFunQuiz

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install azd
        uses: Azure/setup-azd@v1

      - name: Log in with Azure (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Provision + Deploy
        run: azd up --no-prompt
        env:
          AZURE_ENV_NAME: pofunquiz-prod
          AZURE_LOCATION: eastus
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

---

## Deployment Pipeline Diagram

```mermaid
flowchart TD
    DEV[Developer pushes to main] --> GHA[GitHub Actions trigger]
    GHA --> LOGIN[OIDC Login to Azure]
    LOGIN --> PROV[azd provision\nbicep deployment]
    PROV --> ASP[App Service Plan B1]
    PROV --> APP[App Service\napp-pofunquiz-token]
    PROV --> STOR[Storage Account\n+ Tables]
    PROV --> ROLES[RBAC role assignments\nManaged Identity → Storage]
    APP --> DEPLOY[azd deploy\ndotnet publish → zip deploy]
    DEPLOY --> LIVE[App live at\nhttps://app-pofunquiz-token.azurewebsites.net]

    LIVE --> HEALTH[/health check passes]
    LIVE --> APPINS[Telemetry flows to\nApplication Insights]
```

---

## Monitoring

| Signal | Source | Destination |
|--------|--------|-------------|
| Structured logs | Serilog → Console + File + OTLP | Application Insights → Log Analytics |
| Traces | OpenTelemetry `ActivitySource("PoFunQuiz")` | Application Insights |
| Metrics | OpenTelemetry runtime + ASP.NET | Application Insights |
| Request logs | Serilog request logging middleware | Application Insights |
| Health status | `/health` endpoint | Azure App Service health check probe |

### Key Dashboards to Create

1. **Question Generation Latency** — filter traces by `quiz.duration_ms`
2. **Leaderboard Write Failures** — filter `POST /api/leaderboard` 4xx/5xx
3. **SignalR Connection Count** — active hub connections
4. **App Availability** — ping `/health` from Azure Monitor
