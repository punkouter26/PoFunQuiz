@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

// Create a shared Linux App Service Plan in PoShared resource group
resource sharedLinuxAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'PoSharedLinuxAppServicePlan'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'linux'
  properties: {
    reserved: true // This makes it a Linux plan
  }
  tags: tags
}

// Output the plan ID for reference
output appServicePlanId string = sharedLinuxAppServicePlan.id
output appServicePlanName string = sharedLinuxAppServicePlan.name
