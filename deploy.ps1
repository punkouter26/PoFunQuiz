# Deploy PoFunQuiz Application to Azure
# This script deploys the PoFunQuiz application to Azure using the Bicep template

# Configuration
$subscriptionId = "f0504e26-451a-4249-8fb3-46270defdd5b"
$resourceGroupName = "PoFunQuiz"
$location = "eastus" # Change if needed
$bicepFile = "./main.bicep"
$deploymentName = "PoFunQuiz-Deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$publishFolder = "./publish"
$webAppName = ""

# Create logs folder
$logFolder = "./logs"
if (-not (Test-Path -Path $logFolder)) {
    New-Item -ItemType Directory -Path $logFolder
}
$logFile = "$logFolder/azure-deployment-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').log"

function Write-Log {
    param(
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Tee-Object -FilePath $logFile -Append
}

# Start Deployment Process
Write-Log "Starting deployment of PoFunQuiz application to Azure"

# Check if Azure CLI is installed
try {
    $azVersion = az --version
    Write-Log "Azure CLI is installed: $($azVersion[0])"
} catch {
    Write-Log "ERROR: Azure CLI is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
try {
    $accountInfo = az account show | ConvertFrom-Json
    Write-Log "Logged in to Azure as: $($accountInfo.user.name)"
} catch {
    Write-Log "Not logged in to Azure. Attempting to log in..."
    az login
    $accountInfo = az account show | ConvertFrom-Json
    Write-Log "Logged in to Azure as: $($accountInfo.user.name)"
}

# Set the subscription
Write-Log "Setting subscription to: $subscriptionId"
az account set --subscription $subscriptionId

# Check if resource group exists
$resourceGroup = az group show --name $resourceGroupName --query "name" 2>$null
if (-not $resourceGroup) {
    Write-Log "Resource group $resourceGroupName does not exist. Creating it..."
    az group create --name $resourceGroupName --location $location
    Write-Log "Resource group $resourceGroupName created successfully."
} else {
    Write-Log "Resource group $resourceGroupName already exists."
}

# Deploy the Bicep template
try {
    Write-Log "Deploying Bicep template: $bicepFile"
    $deployOutput = az deployment group create `
        --resource-group $resourceGroupName `
        --name $deploymentName `
        --template-file $bicepFile `
        --query "properties.outputs" `
        --verbose | ConvertFrom-Json

    # Extract outputs
    $webAppName = $deployOutput.webAppName.value
    $webAppUrl = $deployOutput.webAppUrl.value
    $storageAccountName = $deployOutput.storageAccountName.value
    $storageAccountConnectionString = $deployOutput.storageAccountConnectionString.value
    $appInsightsConnectionString = $deployOutput.appInsightsConnectionString.value

    Write-Log "Deployed web app: $webAppName"
    Write-Log "Web app URL: $webAppUrl"
    Write-Log "Storage account: $storageAccountName"

} catch {
    Write-Log "ERROR: Failed to deploy Bicep template: $_"
    exit 1
}

# Build and publish the application
try {
    Write-Log "Building and publishing the application"
    
    # Clean the publish directory if it exists
    if (Test-Path -Path $publishFolder) {
        Remove-Item -Path $publishFolder -Recurse -Force
    }

    # Build and publish
    Write-Log "Building and publishing the application with dotnet publish"
    dotnet publish ./PoFunQuiz.sln -c Release -o $publishFolder

    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERROR: Failed to build and publish the application"
        exit 1
    }

    Write-Log "Application built and published successfully to $publishFolder"
    
    # Create a zip file for deployment
    $zipPath = "$publishFolder/site.zip"
    Write-Log "Creating zip file for deployment: $zipPath"
    
    # Remove the zip file if it exists and wait a moment
    if (Test-Path -Path $zipPath) {
        Remove-Item -Path $zipPath -Force
        Start-Sleep -Seconds 2
    }
    
    # Create the zip file using Compress-Archive
    Compress-Archive -Path "$publishFolder/*" -DestinationPath $zipPath -Force
    
    Write-Log "Deployment package created: $zipPath"

    # Deploy the application to Azure
    Write-Log "Deploying application to Azure Web App: $webAppName"
    
    az webapp deployment source config-zip `
        --resource-group $resourceGroupName `
        --name $webAppName `
        --src $zipPath

    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERROR: Failed to deploy application to Azure Web App"
        exit 1
    }
    
    Write-Log "Application deployed successfully to Azure Web App: $webAppName"
} catch {
    Write-Log "ERROR: Failed to build and publish the application: $_"
    exit 1
}

# Restart the web app to apply changes
try {
    Write-Log "Restarting web app to apply changes"
    az webapp restart --name $webAppName --resource-group $resourceGroupName
    Write-Log "Web app restarted successfully"
} catch {
    Write-Log "ERROR: Failed to restart web app: $_"
    # Continue anyway
}

Write-Log "Deployment completed successfully!"
Write-Log "You can access your application at: $webAppUrl"
Write-Log "Remember to check the application settings to ensure the OpenAI API key is correctly configured."