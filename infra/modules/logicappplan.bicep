param project string
param location string = resourceGroup().location
param skuName string
param skuTier string

resource workflow_service_plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${project}-logicapp-plan'
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: 1
  }
  kind: 'elastic'
}

output workflowServicePlanId string = workflow_service_plan.id
