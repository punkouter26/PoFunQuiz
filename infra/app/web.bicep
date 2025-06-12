param name string
param location string = resourceGroup().location
param tags object = {}

param appServicePlanId string
param applicationInsightsName string = ''
param appSettings object = {}
param runtimeName string 
param runtimeVersion string

resource web 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  kind: 'app'
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      linuxFxVersion: '${runtimeName}|${runtimeVersion}'
      alwaysOn: false
      appSettings: [for key in keys(appSettings): {
        name: key
        value: appSettings[key]
      }]
    }
    httpsOnly: true
  }

  identity: {
    type: 'SystemAssigned'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(applicationInsightsName)) {
  name: applicationInsightsName
}

output identityClientId string = web.identity.principalId
output name string = web.name
output uri string = 'https://${web.properties.defaultHostName}'
