targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

var tags = {
  'azd-env-name': environmentName
  'app-name': 'PoFunQuiz'
}

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'PoFunQuiz-rg'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_RESOURCE_POFUNQUIZ_SERVER_ID string = resources.outputs.AZURE_RESOURCE_POFUNQUIZ_SERVER_ID
output SERVICE_POFUNQUIZ_ENDPOINT string = resources.outputs.SERVICE_POFUNQUIZ_ENDPOINT
output AZURE_STORAGE_ACCOUNT_NAME string = resources.outputs.AZURE_STORAGE_ACCOUNT_NAME
output APPLICATIONINSIGHTS_CONNECTION_STRING string = resources.outputs.APPLICATIONINSIGHTS_CONNECTION_STRING
