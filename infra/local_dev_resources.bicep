param project string
param location string = resourceGroup().location
var storageAccountName = toLower(replace('${project}devstg', '-', '')) // 24 character limit!

module sbus 'modules/servicebus.bicep' = {
  name: 'sbus'
  scope: resourceGroup()
  params: {
    project: project
    location: location
    skuName: 'Standard'
  }
}

module storage 'modules/storage.bicep' = {
  name: 'workflowstorage'
  scope: resourceGroup()
  params: {
    name: storageAccountName
    location: location
    skuName: 'Standard_LRS'
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  scope: resourceGroup()
  params: {
    name: '${project}-kv'
    location: location
    tenantId: subscription().tenantId
  }
}
