@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Id of the user or app to assign application roles')
param principalId string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Azure Key Vault endpoint in PoShared resource group')
param keyVaultEndpoint string = ''

@description('Azure Storage Table endpoint for the app storage account')
param storageTableEndpoint string = ''

var resourceToken = uniqueString(resourceGroup().id)

// ============================================================
// Reference shared PoShared resources (managed identity, log analytics)
// ============================================================
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: 'mi-poshared-apps'
  scope: resourceGroup('PoShared')
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: 'law-poshared'
  scope: resourceGroup('PoShared')
}

// ============================================================
// App Service Plan — B1 tier (lowest paid tier; supports always-on + custom domains)
// Use Free (F1) for dev/test by changing skuName to 'F1'
// ============================================================
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-pofunquiz-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true   // required for Linux
  }
}

// ============================================================
// App Service — hosts the Blazor .NET 10 web app
// Managed Identity assigned for Key Vault + Storage access
// ============================================================
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-pofunquiz-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'AZURE_KEY_VAULT_ENDPOINT'
          value: keyVaultEndpoint
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
        {
          // Table storage endpoint — Managed Identity auth (no connection string key needed)
          name: 'ConnectionStrings__tables'
          value: storageTableEndpoint
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(VaultName=${split(keyVaultEndpoint, '.')[0]};SecretName=ApplicationInsights-ConnectionString)'
        }
      ]
    }
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_APP_SERVICE_NAME string = appService.name
output AZURE_APP_SERVICE_URL string = 'https://${appService.properties.defaultHostName}'
