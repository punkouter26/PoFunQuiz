@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

// Reference existing shared resources in PoShared resource group
resource existingApplicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: 'PoSharedApplicationInsights'
  scope: resourceGroup('PoShared')
}

resource existingWindowsAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' existing = {
  name: 'PoSharedAppServicePlan'
  scope: resourceGroup('PoShared')
}

// App Service for PoFunQuiz (Windows)
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: 'PoFunQuiz'
  location: location
  kind: 'app'
  tags: union(tags, { 'azd-service-name': 'PoFunQuiz' })
  properties: {
    serverFarmId: existingWindowsAppServicePlan.id
    siteConfig: {
      windowsFxVersion: 'DOTNET|9'
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: true
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
          value: 'DefaultEndpointsProtocol=https;AccountName=posharedtablestorage;AccountKey=LPMW8tZgPJJnYLhCRME+KW4fViv3rJZA+XrycWCjO89yFOMaE2Qi7m3IAkb5dtdD8cR6SFk478b++ASt5ZuqfA==;EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'AppSettings__Storage__BlobStorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=posharedtablestorage;AccountKey=LPMW8tZgPJJnYLhCRME+KW4fViv3rJZA+XrycWCjO89yFOMaE2Qi7m3IAkb5dtdD8cR6SFk478b++ASt5ZuqfA==;EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'AppSettings__Storage__DefaultContainer'
          value: 'quiz-data'
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
