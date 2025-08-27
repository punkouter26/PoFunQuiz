@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

param pofunquizServerExists bool

@description('Id of the user or app to assign application roles')
param principalId string

@description('Principal type of user or app')
param principalType string

// Reference existing shared resources
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' existing = {
  name: 'posharedappserviceplan'
  scope: resourceGroup('PoShared')
}

resource existingApplicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: 'posharedappinsights'
  scope: resourceGroup('PoShared')
}

resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: 'posharedloganalyticsworkspace'
  scope: resourceGroup('PoShared')
}

// App Service for PoFunQuiz
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'PoFunQuiz'
  location: location
  kind: 'app'
  tags: union(tags, { 'azd-service-name': 'PoFunQuiz' })
  properties: {
    serverFarmId: existingAppServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      http20Enabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: existingApplicationInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'AppSettings__Storage__TableStorageConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=https://posharedkeyvault.vault.azure.net/secrets/TableStorageConnectionString/)'
        }
        {
          name: 'AppSettings__Storage__BlobStorageConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=https://posharedkeyvault.vault.azure.net/secrets/BlobStorageConnectionString/)'
        }
        {
          name: 'AppSettings__Storage__DefaultContainer'
          value: 'quiz-data'
        }
        {
          name: 'AppSettings__AzureOpenAI__Endpoint'
          value: '@Microsoft.KeyVault(SecretUri=https://posharedkeyvault.vault.azure.net/secrets/AzureOpenAIEndpoint/)'
        }
        {
          name: 'AppSettings__AzureOpenAI__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=https://posharedkeyvault.vault.azure.net/secrets/AzureOpenAIApiKey/)'
        }
        {
          name: 'AppSettings__AzureOpenAI__DeploymentName'
          value: 'gpt-4o'
        }
        {
          name: 'AppSettings__Logging__ApplicationInsightsKey'
          value: existingApplicationInsights.properties.InstrumentationKey
        }
        {
          name: 'AppSettings__Logging__MinimumLevel'
          value: 'Information'
        }
        {
          name: 'AppSettings__Logging__EnableConsoleLogging'
          value: 'false'
        }
      ]
    }
    httpsOnly: true
    clientAffinityEnabled: false
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Output the App Service resource ID
output AZURE_RESOURCE_POFUNQUIZ_SERVER_ID string = appService.id
