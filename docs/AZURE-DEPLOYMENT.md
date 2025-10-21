# Azure Deployment Guide - PoFunQuiz

This document provides instructions for deploying PoFunQuiz to Azure using Azure Developer CLI (azd).

## Prerequisites

- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) installed
- [Azure CLI (az)](https://learn.microsoft.com/cli/azure/install-azure-cli) installed
- An Azure subscription
- .NET 9.0 SDK installed

## Architecture Overview

### Resource Groups

**PoFunQuiz** (New - Created by deployment):
- Application Insights (`PoFunQuiz`)
- Log Analytics Workspace (`PoFunQuiz`)
- Storage Account (`pofunquiz`)
  - Table Storage (Players, Game Sessions)
- App Service (`PoFunQuiz`)

**PoShared** (Existing - Referenced):
- App Service Plan (`PoSharedAppServicePlan`) - Shared across apps
- Azure OpenAI (`posharedopenaieastus`) - Shared GPT-4o deployment

### Local vs Azure

| Resource | Local Development | Azure Production |
|----------|------------------|------------------|
| Table Storage | Azurite (`UseDevelopmentStorage=true`) | Azure Storage Account |
| Blob Storage | Azurite | Azure Storage Account |
| Azure OpenAI | Shared resource | Shared resource |
| Application Insights | Not used (local logs) | Azure App Insights |

## Quick Start - Deploy to Azure

### 1. Login to Azure

```powershell
azd auth login
az login
```

### 2. Initialize Environment

```powershell
# Initialize azd (first time only)
azd init
```

When prompted:
- **Environment name**: Enter a unique name (e.g., `pofunquiz-prod`)
- **Azure subscription**: Select your subscription
- **Azure location**: `eastus2` (or your preferred region)

### 3. Deploy

```powershell
# Deploy everything (infrastructure + app) with one command
azd up
```

This command will:
1. ✅ Create the `PoFunQuiz` resource group
2. ✅ Create Log Analytics Workspace
3. ✅ Create Application Insights
4. ✅ Create Storage Account with tables
5. ✅ Create App Service
6. ✅ Build the .NET application
7. ✅ Deploy the application to App Service

### 4. Verify Deployment

```powershell
# Get the deployed app URL
azd show
```

Visit the URL displayed to see your live app!

## Alternative Deployment Commands

### Deploy Infrastructure Only

```powershell
# Provision Azure resources without deploying code
azd provision
```

### Deploy Code Only

```powershell
# Deploy app code to existing infrastructure
azd deploy
```

### Clean Up Resources

```powershell
# Delete all Azure resources (keeps local code)
azd down
```

## Configuration

### Hard-Coded Values (No User Input Required)

The following are configured in `infra/resources.bicep`:
- ✅ Resource Group: `PoFunQuiz`
- ✅ Location: `eastus2`
- ✅ Storage Account: `pofunquiz`
- ✅ App Service Plan: Uses existing `PoSharedAppServicePlan`
- ✅ Azure OpenAI: Uses existing `posharedopenaieastus`

### Environment Variables

The deployment automatically configures these app settings:

| Setting | Source | Description |
|---------|--------|-------------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Auto-generated | App Insights connection |
| `AppSettings__Storage__TableStorageConnectionString` | Auto-generated | Storage account key |
| `AppSettings__AzureOpenAI__Endpoint` | Hard-coded | Shared OpenAI endpoint |
| `AppSettings__AzureOpenAI__ApiKey` | Hard-coded | Shared OpenAI API key |
| `AppSettings__AzureOpenAI__DeploymentName` | Hard-coded | `gpt-4o` |

## Local Development

### Required Configuration

File: `PoFunQuiz.Server/appsettings.Development.json`

```json
{
  "AppSettings": {
    "Storage": {
      "TableStorageConnectionString": "UseDevelopmentStorage=true",
      "BlobStorageConnectionString": "UseDevelopmentStorage=true"
    },
    "AzureOpenAI": {
      "Endpoint": "https://posharedopenaieastus.openai.azure.com/",
      "ApiKey": "your-key-here",
      "DeploymentName": "gpt-4o"
    }
  }
}
```

### Start Azurite

```powershell
# Start Azurite for local Table Storage emulation
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

### Run Locally

```powershell
dotnet run --project PoFunQuiz.Server
```

Visit: `https://localhost:5001`

## Cost Optimization

All resources use the cheapest/free tiers:

| Resource | Tier | Cost |
|----------|------|------|
| App Service Plan | Shared (PoShared) | Shared cost |
| Storage Account | Standard LRS | ~$0.02/GB/month |
| Log Analytics | PerGB2018 (1GB cap) | First 5GB free |
| Application Insights | Free | First 5GB free |
| Azure OpenAI | Shared (PoShared) | Shared cost |

**Estimated Monthly Cost**: < $5 (excluding shared resources)

## Troubleshooting

### Issue: "PoSharedAppServicePlan not found"

**Solution**: The deployment requires an existing App Service Plan named `PoSharedAppServicePlan` in the `PoShared` resource group. If this doesn't exist, you'll need to:

1. Create the PoShared resource group
2. Create the App Service Plan manually, or
3. Update `infra/resources.bicep` to create a new plan

### Issue: Storage account name conflict

**Error**: `The storage account name 'pofunquiz' is already taken`

**Solution**: Storage account names must be globally unique. Update the name in `infra/resources.bicep`:

```bicep
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'pofunquiz2024' // Change to unique name
  // ...
}
```

### Issue: Build warnings about Radzen components

These are cosmetic warnings about missing `@using` directives for Radzen.Blazor components. They don't affect functionality and can be ignored.

## CI/CD with GitHub Actions

The deployment can be automated with GitHub Actions:

```yaml
# .github/workflows/azure-dev.yml
name: Azure Dev Deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Install azd
        uses: Azure/setup-azd@v1.0.0
      
      - name: Azure Login
        run: |
          azd auth login \
            --client-id "${{secrets.AZURE_CLIENT_ID}}" \
            --client-secret "${{secrets.AZURE_CLIENT_SECRET}}" \
            --tenant-id "${{secrets.AZURE_TENANT_ID}}"
      
      - name: Deploy
        run: azd up --no-prompt
        env:
          AZURE_ENV_NAME: ${{ vars.AZURE_ENV_NAME }}
          AZURE_LOCATION: ${{ vars.AZURE_LOCATION }}
          AZURE_SUBSCRIPTION_ID: ${{ vars.AZURE_SUBSCRIPTION_ID }}
```

## Monitoring

### View Application Logs

```powershell
# Stream live logs from App Service
az webapp log tail --name PoFunQuiz --resource-group PoFunQuiz
```

### View Application Insights

1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to: Resource Groups → PoFunQuiz → PoFunQuiz (Application Insights)
3. View telemetry, requests, dependencies, and failures

### Health Check

Visit: `https://pofunquiz.azurewebsites.net/api/health`

## Next Steps

1. ✅ Deploy to Azure: `azd up`
2. ✅ Verify health: `/api/health` endpoint
3. ✅ Run integration tests against production
4. ✅ Set up GitHub Actions for CI/CD
5. ✅ Configure custom domain (optional)
6. ✅ Enable App Service authentication (optional)

## Support

For issues or questions:
- Review: `AGENTS.md` for architecture guidelines
- Review: `docs/TESTING.md` for test documentation
- Check: Azure Portal for resource status
- Review: Application Insights for telemetry
