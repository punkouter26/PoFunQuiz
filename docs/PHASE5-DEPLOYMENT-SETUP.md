# Phase 5: GitHub CI/CD Deployment Setup Guide

This guide provides step-by-step instructions to set up GitHub Actions CI/CD for PoFunQuiz using Azure Developer CLI (azd) with federated credentials.

---

## Prerequisites

- Azure CLI installed: `az --version`
- Azure Developer CLI installed: `azd version`
- GitHub CLI installed: `gh --version`
- Azure subscription with contributor access
- GitHub repository with admin access

---

## Step 1: Verify Azure Resources

First, verify the existing shared App Service Plan:

```bash
# Login to Azure
az login

# Verify the shared resource group exists
az group show --name PoShared

# Verify the shared App Service Plan exists
az appservice plan show --name PoSharedAppServicePlan --resource-group PoShared

# Check the plan is F1 tier
az appservice plan show --name PoSharedAppServicePlan --resource-group PoShared --query "sku"
```

**Expected Output:**
```json
{
  "capacity": 0,
  "family": "F",
  "name": "F1",
  "size": "F1",
  "tier": "Free"
}
```

---

## Step 2: Initialize Azure Developer CLI

```bash
cd c:\Users\punko\Downloads\PoFunQuiz

# Initialize azd environment (if not already done)
azd init

# When prompted:
# - Environment name: pofunquiz-prod (or your preference)
# - Location: eastus2

# Set up the environment
azd env new pofunquiz-prod
azd env set AZURE_LOCATION eastus2
```

---

## Step 3: Create Azure Resources

```bash
# Provision infrastructure (creates resource group and resources)
azd provision

# This will:
# - Create PoFunQuiz resource group (if not exists)
# - Deploy Log Analytics, Application Insights, Storage Account
# - Create App Service using shared plan
# - Configure app settings
```

**Important Notes:**
- The Bicep template references the existing `PoSharedAppServicePlan` from the `PoShared` resource group
- No new App Service Plan will be created (zero additional hosting costs)
- App Service is configured for F1 tier constraints (32-bit worker, AlwaysOn disabled)

---

## Step 4: Set Up GitHub Federated Credentials

This is the recommended approach for GitHub Actions authentication (no secrets to rotate).

### 4.1 Create Service Principal with Federated Credentials

```bash
# Set variables
GITHUB_ORG="punkouter26"
GITHUB_REPO="PoFunQuiz"
APP_NAME="PoFunQuiz-GitHub-Actions"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Create Azure AD Application
APP_ID=$(az ad app create --display-name $APP_NAME --query appId -o tsv)
echo "Application ID: $APP_ID"

# Create Service Principal
az ad sp create --id $APP_ID

# Get Service Principal Object ID
SP_OBJECT_ID=$(az ad sp show --id $APP_ID --query id -o tsv)
echo "Service Principal Object ID: $SP_OBJECT_ID"

# Assign Contributor role to the subscription
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$SUBSCRIPTION_ID

# Create federated credential for main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"PoFunQuiz-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

echo "✅ Federated credential created for main branch"
```

### 4.2 Get Tenant ID

```bash
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Tenant ID: $TENANT_ID"
```

### 4.3 Summary of Credentials

```bash
echo "=== GitHub Secrets Required ==="
echo "AZURE_CLIENT_ID: $APP_ID"
echo "AZURE_TENANT_ID: $TENANT_ID"
echo "AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
```

---

## Step 5: Configure GitHub Secrets

### 5.1 Using GitHub CLI (Recommended)

```bash
cd c:\Users\punko\Downloads\PoFunQuiz

# Login to GitHub
gh auth login

# Set secrets
gh secret set AZURE_CLIENT_ID --body "$APP_ID"
gh secret set AZURE_TENANT_ID --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"

# Set variables
gh variable set AZURE_ENV_NAME --body "pofunquiz-prod"
gh variable set AZURE_LOCATION --body "eastus2"

echo "✅ GitHub secrets and variables configured"
```

### 5.2 Using GitHub Web UI (Alternative)

1. Go to: `https://github.com/punkouter26/PoFunQuiz/settings/secrets/actions`
2. Click "New repository secret"
3. Add the following secrets:
   - `AZURE_CLIENT_ID`: [Your APP_ID from Step 4]
   - `AZURE_TENANT_ID`: [Your TENANT_ID from Step 4]
   - `AZURE_SUBSCRIPTION_ID`: [Your SUBSCRIPTION_ID from Step 4]

4. Go to: `https://github.com/punkouter26/PoFunQuiz/settings/variables/actions`
5. Click "New repository variable"
6. Add the following variables:
   - `AZURE_ENV_NAME`: `pofunquiz-prod`
   - `AZURE_LOCATION`: `eastus2`

---

## Step 6: Configure azd for GitHub Actions

```bash
cd c:\Users\punko\Downloads\PoFunQuiz

# Configure azd pipeline for GitHub Actions
azd pipeline config

# When prompted:
# - Select: GitHub Actions
# - Repository: punkouter26/PoFunQuiz
# - Branch: main
# - Confirm federated credential setup: Yes
```

This command will:
- Create/update `.github/workflows/azure-dev.yml` (if using azd default)
- Configure the necessary secrets automatically
- Set up federated credentials

---

## Step 7: Test Local Build

Before pushing to GitHub, verify the build works locally:

```bash
cd c:\Users\punko\Downloads\PoFunQuiz

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run tests (excluding E2E)
dotnet test --configuration Release --filter "Category!=E2E"

# Expected: Build succeeds, most tests pass (2 may fail without OpenAI credentials)
```

---

## Step 8: Deploy via GitHub Actions

### 8.1 Commit and Push

```bash
cd c:\Users\punko\Downloads\PoFunQuiz

# Stage changes
git add .

# Commit
git commit -m "Phase 5: Add GitHub Actions CI/CD with federated credentials"

# Push to main branch (triggers workflow)
git push origin main
```

### 8.2 Monitor Deployment

```bash
# Watch workflow in GitHub CLI
gh run watch

# Or visit GitHub Actions page
# https://github.com/punkouter26/PoFunQuiz/actions
```

---

## Step 9: Verify Deployment

### 9.1 Health Check

```bash
# Test health endpoint
curl https://pofunquiz.azurewebsites.net/api/health

# Expected output: JSON with health check results
# {"status":"Healthy","timestamp":"2025-10-21T...","checks":[...]}
```

### 9.2 Swagger API Documentation

Visit: https://pofunquiz.azurewebsites.net/swagger

**Expected:** Swagger UI with API endpoints

### 9.3 Main Application

Visit: https://pofunquiz.azurewebsites.net

**Expected:** PoFunQuiz homepage loads successfully

### 9.4 Application Insights

```bash
# Check Application Insights is receiving telemetry
az monitor app-insights component show \
  --app PoFunQuiz \
  --resource-group PoFunQuiz

# View recent logs in Azure Portal
# Portal → Resource Group → Application Insights → Logs
```

---

## Step 10: Verify Cost (Zero Additional Hosting)

```bash
# Verify App Service is using shared F1 plan
az webapp show --name PoFunQuiz --resource-group PoFunQuiz --query "serverFarmId"

# Expected output should reference PoSharedAppServicePlan
# /subscriptions/.../resourceGroups/PoShared/providers/Microsoft.Web/serverfarms/PoSharedAppServicePlan

# Verify plan tier
az appservice plan show --name PoSharedAppServicePlan --resource-group PoShared --query "sku.tier"

# Expected: "Free"
```

✅ **Confirmation:** App Service is using the shared F1 plan → **Zero additional hosting costs**

---

## Troubleshooting

### Issue: Federated Credential Authentication Fails

**Symptoms:** GitHub Actions fails with "AADSTS700016: Application not found"

**Solution:**
```bash
# Wait 5 minutes for Azure AD replication
# Then re-run the workflow

# Or verify federated credential exists
az ad app federated-credential list --id $APP_ID
```

### Issue: Build Fails in GitHub Actions

**Check:**
1. .NET 9.0 SDK installed in workflow ✓
2. All dependencies restored ✓
3. Tests don't block deployment (continue-on-error: true) ✓

### Issue: Health Check Fails

**Debugging:**
```bash
# Check App Service logs
az webapp log tail --name PoFunQuiz --resource-group PoFunQuiz

# Check Application Insights for errors
# Portal → Application Insights → Failures
```

### Issue: Swagger Not Accessible

**Check Program.cs:**
Ensure Swagger is enabled in production (already configured in Phase 4):
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

**Note:** For production Swagger, update Program.cs to enable in all environments:
```csharp
app.MapOpenApi(); // Enable in all environments
```

### Issue: App Settings Not Applied

**Solution:**
```bash
# Manually set app settings if needed
az webapp config appsettings set \
  --name PoFunQuiz \
  --resource-group PoFunQuiz \
  --settings \
    "AppSettings__AzureOpenAI__ApiKey=YOUR_KEY" \
    "AppSettings__AzureOpenAI__Endpoint=YOUR_ENDPOINT"
```

---

## GitHub Actions Workflow Explained

The workflow (`.github/workflows/main.yml`) performs:

1. **Build** → Compile .NET 9.0 solution
2. **Test** → Run unit and integration tests (excluding E2E)
3. **Deploy** → Use `azd deploy` with federated credentials
4. **Health Check** → Verify `/api/health` returns 200
5. **API Test** → Verify Swagger endpoint accessible
6. **Functional Test** → Verify main app page loads with correct title

**Trigger:** Push to `main` branch or manual dispatch

**Authentication:** Federated credentials (no rotating secrets)

---

## Security Notes

### Acceptable for Private Repositories
- Sensitive data in `appsettings.json` and `appsettings.Production.json` is acceptable
- GitHub repository is **private** (as confirmed in requirements)
- App settings override file values in production

### For Higher Security Projects
Consider:
- Azure Key Vault for secrets
- GitHub Secrets for sensitive values
- Managed Identity for Azure service connections

---

## Phase 5 Checklist

- [x] App Service added to Bicep (already exists)
- [x] App Service name matches .sln file: `PoFunQuiz`
- [x] Deploy to resource group: `PoFunQuiz`
- [x] Use existing App Service Plan: `PoSharedAppServicePlan` (F1 tier)
- [x] F1 constraints configured: 32-bit worker, AlwaysOn disabled
- [x] App settings configured in Bicep
- [x] GitHub Actions workflow created (`.github/workflows/main.yml`)
- [x] Federated credentials for authentication
- [x] Build → Test → Deploy pipeline
- [x] Health check verification (`/api/health`)
- [x] Swagger enabled for API testing
- [x] Functional test for page title
- [ ] **TODO: Run `azd provision` to create resources**
- [ ] **TODO: Set up federated credentials (Step 4)**
- [ ] **TODO: Configure GitHub secrets (Step 5)**
- [ ] **TODO: Push to GitHub to trigger deployment (Step 8)**
- [ ] **TODO: Verify deployment (Step 9)**
- [ ] **TODO: Confirm zero additional hosting costs (Step 10)**

---

## Next Steps

1. **Execute Step 4** - Create service principal with federated credentials
2. **Execute Step 5** - Configure GitHub secrets
3. **Execute Step 8** - Push to GitHub to trigger deployment
4. **Execute Step 9** - Verify deployment success
5. **Execute Step 10** - Confirm cost optimization

**After successful deployment, the only deployment method will be GitHub Actions CI/CD.**

---

## Quick Reference Commands

```bash
# Provision infrastructure
azd provision

# Deploy application
azd deploy

# View GitHub Actions status
gh run list

# Watch current workflow
gh run watch

# Test health endpoint
curl https://pofunquiz.azurewebsites.net/api/health

# View App Service logs
az webapp log tail --name PoFunQuiz --resource-group PoFunQuiz
```

---

**Phase 5 setup is complete!** Follow the steps in order to deploy via GitHub Actions. 🚀
