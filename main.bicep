// PoFunQuiz Azure Infrastructure Deployment
// Target: https://portal.azure.com/#@punkouter25outlook.onmicrosoft.com/resource/subscriptions/f0504e26-451a-4249-8fb3-46270defdd5b/resourceGroups/PoFunQuiz/overview

// Parameters with default values
@description('The environment name. Default: dev')
param environmentName string = 'dev'

@description('The Azure region for deploying web app. Default: Canada Central')
param webAppLocation string = 'Canada Central'

@description('The name of the application. Default: pofunquiz')
param appName string = 'pofunquiz'

// Tags for all resources
var tags = {
  application: appName
  environment: environmentName
}

// Names for resources based on naming convention
var webAppName = '${appName}-webapp-${environmentName}'
var appInsightsName = '${appName}-insights-${environmentName}'
var storageAccountName = '${replace(appName, '-', '')}storage${environmentName}'

// Reference to existing App Service Plan
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2022-03-01' existing = {
  name: 'PoSharedFree'
  scope: resourceGroup('PoShared')
}

// Reference to existing Application Insights
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// Reference to existing Storage Account
resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccountName
}

// Web App - This is the main resource we're managing
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: webAppLocation
  tags: tags
  properties: {
    serverFarmId: existingAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        // Application Insights settings
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: existingAppInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights:InstrumentationKey'
          value: existingAppInsights.properties.InstrumentationKey
        }
        // Storage settings
        {
          name: 'AzureTableStorage:ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${existingStorageAccount.name};AccountKey=${existingStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'AzureTableStorage:TableName'
          value: 'PlayerStats'
        }
        // OpenAI settings (using shared endpoint)
        {
          name: 'OpenAI:Endpoint'
          value: 'https://poshared.openai.azure.com/'
        }
        {
          name: 'OpenAI:DeploymentName'
          value: 'gpt-35-turbo'
        }
        // App specific settings
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Define outputs for important resource information
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
