param location string = resourceGroup().location
param tags object = {}
param principalId string = ''

// Existing shared App Service Plan
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' existing = {
  name: 'PoShared'
  scope: resourceGroup('PoShared')
}

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'PoFunQuiz'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: 1
    }
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'PoFunQuiz'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Storage Account
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'pofunquiz'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storage
  name: 'default'
}

resource playersTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableService
  name: 'PoFunQuizPlayers'
}

resource sessionsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableService
  name: 'PoFunQuizGameSessions'
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'PoFunQuiz'
  location: location
  tags: union(tags, { 'azd-service-name': 'PoFunQuiz' })
  properties: {
    serverFarmId: existingAppServicePlan.id
    siteConfig: {
      windowsFxVersion: 'DOTNET|9'
      alwaysOn: false
      use32BitWorkerProcess: true  // Required for F1 tier
      netFrameworkVersion: 'v9.0'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'AppSettings__Storage__TableStorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'AppSettings__AzureOpenAI__Endpoint'
          value: 'https://posharedopenaieastus.openai.azure.com/'
        }
        {
          name: 'AppSettings__AzureOpenAI__ApiKey'
          value: '3034cc85dd024ca29155d4534911df9f'
        }
        {
          name: 'AppSettings__AzureOpenAI__DeploymentName'
          value: 'gpt-4o'
        }
      ]
    }
    httpsOnly: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output AZURE_RESOURCE_POFUNQUIZ_SERVER_ID string = appService.id
output SERVICE_POFUNQUIZ_ENDPOINT string = 'https://${appService.properties.defaultHostName}'
output AZURE_STORAGE_ACCOUNT_NAME string = storage.name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
