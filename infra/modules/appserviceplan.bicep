param skuName string
param skuTier string
param location string = resourceGroup().location
param project string

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: '${project}-web-plan'
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
}

output appServicePlanId string = appServicePlan.id
