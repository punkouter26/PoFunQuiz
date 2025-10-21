# Phase 5 Quick Setup Script
# This script automates the federated credentials setup for GitHub Actions

param(
    [Parameter(Mandatory=$false)]
    [string]$GitHubOrg = "punkouter26",
    
    [Parameter(Mandatory=$false)]
    [string]$GitHubRepo = "PoFunQuiz",
    
    [Parameter(Mandatory=$false)]
    [string]$AppName = "PoFunQuiz-GitHub-Actions",
    
    [Parameter(Mandatory=$false)]
    [string]$AzureEnvName = "pofunquiz-prod",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2"
)

Write-Host "=== Phase 5: GitHub CI/CD Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

$azcli = Get-Command az -ErrorAction SilentlyContinue
if (-not $azcli) {
    Write-Host "❌ Azure CLI not found. Please install: https://aka.ms/InstallAzureCLI" -ForegroundColor Red
    exit 1
}

$azd = Get-Command azd -ErrorAction SilentlyContinue
if (-not $azd) {
    Write-Host "❌ Azure Developer CLI not found. Please install: https://aka.ms/install-azd" -ForegroundColor Red
    exit 1
}

$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    Write-Host "❌ GitHub CLI not found. Please install: https://cli.github.com" -ForegroundColor Red
    exit 1
}

Write-Host "✅ All prerequisites installed" -ForegroundColor Green
Write-Host ""

# Azure Login
Write-Host "Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Launching login..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "✅ Logged in to Azure subscription: $($account.name)" -ForegroundColor Green
$subscriptionId = $account.id
$tenantId = $account.tenantId
Write-Host ""

# GitHub Login
Write-Host "Checking GitHub login..." -ForegroundColor Yellow
$ghUser = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not logged in to GitHub. Launching login..." -ForegroundColor Yellow
    gh auth login
}

Write-Host "✅ Logged in to GitHub" -ForegroundColor Green
Write-Host ""

# Create Azure AD Application
Write-Host "Creating Azure AD Application: $AppName" -ForegroundColor Yellow
$appId = az ad app list --display-name $AppName --query "[0].appId" -o tsv

if ($appId) {
    Write-Host "⚠️  Application already exists: $appId" -ForegroundColor Yellow
    $recreate = Read-Host "Do you want to use the existing app? (Y/N)"
    if ($recreate -eq "N" -or $recreate -eq "n") {
        Write-Host "Please delete the existing app manually or choose a different name." -ForegroundColor Red
        exit 1
    }
} else {
    $appId = az ad app create --display-name $AppName --query appId -o tsv
    Write-Host "✅ Application created: $appId" -ForegroundColor Green
    
    # Create Service Principal
    Write-Host "Creating Service Principal..." -ForegroundColor Yellow
    az ad sp create --id $appId | Out-Null
    Write-Host "✅ Service Principal created" -ForegroundColor Green
    
    # Assign Contributor role
    Write-Host "Assigning Contributor role..." -ForegroundColor Yellow
    az role assignment create --assignee $appId --role Contributor --scope "/subscriptions/$subscriptionId" | Out-Null
    Write-Host "✅ Role assigned" -ForegroundColor Green
}

Write-Host ""

# Create Federated Credential
Write-Host "Creating federated credential for main branch..." -ForegroundColor Yellow
$credName = "PoFunQuiz-main"
$existingCred = az ad app federated-credential list --id $appId --query "[?name=='$credName']" -o tsv

if ($existingCred) {
    Write-Host "⚠️  Federated credential already exists" -ForegroundColor Yellow
} else {
    $fedCredJson = @"
{
    "name": "$credName",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:${GitHubOrg}/${GitHubRepo}:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
}
"@
    
    $fedCredJson | az ad app federated-credential create --id $appId --parameters '@-' | Out-Null
    Write-Host "✅ Federated credential created" -ForegroundColor Green
}

Write-Host ""

# Configure GitHub Secrets
Write-Host "Configuring GitHub secrets..." -ForegroundColor Yellow

gh secret set AZURE_CLIENT_ID --body "$appId" --repo "${GitHubOrg}/${GitHubRepo}"
Write-Host "✅ AZURE_CLIENT_ID set" -ForegroundColor Green

gh secret set AZURE_TENANT_ID --body "$tenantId" --repo "${GitHubOrg}/${GitHubRepo}"
Write-Host "✅ AZURE_TENANT_ID set" -ForegroundColor Green

gh secret set AZURE_SUBSCRIPTION_ID --body "$subscriptionId" --repo "${GitHubOrg}/${GitHubRepo}"
Write-Host "✅ AZURE_SUBSCRIPTION_ID set" -ForegroundColor Green

Write-Host ""

# Configure GitHub Variables
Write-Host "Configuring GitHub variables..." -ForegroundColor Yellow

gh variable set AZURE_ENV_NAME --body "$AzureEnvName" --repo "${GitHubOrg}/${GitHubRepo}"
Write-Host "✅ AZURE_ENV_NAME set" -ForegroundColor Green

gh variable set AZURE_LOCATION --body "$Location" --repo "${GitHubOrg}/${GitHubRepo}"
Write-Host "✅ AZURE_LOCATION set" -ForegroundColor Green

Write-Host ""

# Summary
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Azure Configuration:" -ForegroundColor Cyan
Write-Host "  Client ID: $appId"
Write-Host "  Tenant ID: $tenantId"
Write-Host "  Subscription ID: $subscriptionId"
Write-Host ""
Write-Host "GitHub Configuration:" -ForegroundColor Cyan
Write-Host "  Repository: ${GitHubOrg}/${GitHubRepo}"
Write-Host "  Environment: $AzureEnvName"
Write-Host "  Location: $Location"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run 'azd provision' to create Azure resources"
Write-Host "  2. Commit and push your changes to trigger deployment"
Write-Host "  3. Monitor deployment: gh run watch"
Write-Host "  4. Verify: https://pofunquiz.azurewebsites.net/api/health"
Write-Host ""
Write-Host "For detailed instructions, see: docs/PHASE5-DEPLOYMENT-SETUP.md" -ForegroundColor Cyan
