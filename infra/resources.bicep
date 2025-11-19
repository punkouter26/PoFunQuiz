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
  name: 'PoFunQuiz-logs'
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
  name: 'PoFunQuiz-insights'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'PoFunQuiz-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Storage Account
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'pofunquizstorage'
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

// Store secrets in Key Vault
resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'TableStorageConnectionString'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  }
}

resource openAIApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AzureOpenAI--ApiKey'
  properties: {
    value: 'cf91f330ad8c4338a722d293f15b7cbe'
  }
}

// SignalR Service
resource signalR 'Microsoft.SignalRService/signalR@2023-02-01' = {
  name: 'PoFunQuiz-signalr'
  location: location
  tags: tags
  sku: {
    name: 'Free_F1'
    tier: 'Free'
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
  }
}

resource signalRConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Azure--SignalR--ConnectionString'
  properties: {
    value: signalR.listKeys().primaryConnectionString
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'PoFunQuiz'
  location: location
  tags: union(tags, { 'azd-service-name': 'PoFunQuiz' })
  properties: {
    serverFarmId: existingAppServicePlan.id
    siteConfig: {
      windowsFxVersion: 'DOTNET|10'
      alwaysOn: false
      use32BitWorkerProcess: true  // Required for F1 tier
      netFrameworkVersion: 'v10.0'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'Azure__SignalR__ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=Azure--SignalR--ConnectionString)'
        }
        {
          name: 'AZURE_KEY_VAULT_ENDPOINT'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'AppSettings__Storage__TableStorageConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=TableStorageConnectionString)'
        }
        {
          name: 'AppSettings__AzureOpenAI__Endpoint'
          value: 'https://posharedopenai.openai.azure.com/'
        }
        {
          name: 'AppSettings__AzureOpenAI__ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=AzureOpenAI--ApiKey)'
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

// Grant App Service managed identity access to Key Vault secrets
resource keyVaultSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appService.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output AZURE_RESOURCE_POFUNQUIZ_SERVER_ID string = appService.id
output SERVICE_POFUNQUIZ_ENDPOINT string = 'https://${appService.properties.defaultHostName}'
output AZURE_STORAGE_ACCOUNT_NAME string = storage.name
output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.properties.vaultUri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
