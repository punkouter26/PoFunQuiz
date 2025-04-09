# deploy.ps1 - Script to deploy PoFunQuiz.Web to Azure App Service

# --- Configuration ---
$resourceGroupName = "PoFunQuiz"
$appServiceName = "pofunquiz-webapp-dev" # Corrected App Service name
$solutionFile = "PoFunQuiz.sln" # Assuming solution file is in the root
$webProjectPath = "PoFunQuiz.Web/PoFunQuiz.Web.csproj" # Path to the Blazor project
$publishOutputDir = "./publish_output" # Temporary directory for published files
$zipFileName = "deployment.zip" # Temporary zip file name

# --- Script ---

Write-Host "Starting deployment process for $appServiceName..."

# 1. Build and Publish the Web Project
Write-Host "Building and publishing project: $webProjectPath..."
# Restore first, then publish without restore
dotnet restore $solutionFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet restore failed!"
    exit 1
}
dotnet publish $webProjectPath --configuration Release --output $publishOutputDir --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}
Write-Host "Project published successfully to $publishOutputDir."

# 2. Create Zip Archive
$zipFilePath = Join-Path -Path $PWD -ChildPath $zipFileName
Write-Host "Creating deployment package: $zipFilePath..."
Compress-Archive -Path "$($publishOutputDir)\*" -DestinationPath $zipFilePath -Force
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create zip archive!"
    exit 1
}
Write-Host "Deployment package created successfully."

# 3. Deploy using Azure CLI
Write-Host "Deploying $zipFilePath to App Service $appServiceName in resource group $resourceGroupName..."
az webapp deployment source config-zip --resource-group $resourceGroupName --name $appServiceName --src $zipFilePath --timeout 1200 # Increased timeout
if ($LASTEXITCODE -ne 0) {
    Write-Error "Azure deployment failed!"
    # Don't exit immediately, still try to cleanup
} else {
    Write-Host "Deployment completed successfully."
}

# 4. Cleanup
Write-Host "Cleaning up temporary files..."
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath -Force
    Write-Host "Removed $zipFilePath."
}
if (Test-Path $publishOutputDir) {
    Remove-Item $publishOutputDir -Recurse -Force
    Write-Host "Removed $publishOutputDir directory."
}

Write-Host "Deployment script finished."
