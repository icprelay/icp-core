// pipeline parameters
param project     string
param apiClientId string
param uiClientId  string

param packname string = 'core'

// computed parameters
param location              string = resourceGroup().location
param storageAccountName    string = toLower(replace('${project}logicstorage', '-', ''))
param storageFileShareName  string = '${project}-${packname}-fs'
param logicAppName          string = '${project}-logicapp-${packname}'
param kvname                string = '${project}-keyvault'
param workflowSkuName       string = 'WS1'
param workflowSkuTier       string = 'WorkflowStandard'
param appServiceSkuName     string = 'B1'
param appServiceSkuTier     string = 'Basic'
param serviceBusSkuName     string = 'Standard'

resource storage_account 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
  scope: resourceGroup('icp-persistent')
}

module file_share 'modules/storagefileshare.bicep' = {
  name: storageFileShareName
  scope: resourceGroup('icp-persistent')
  params: {
    storageAccountName:   storageAccountName 
    storageFileShareName: storageFileShareName
  }
}

module app_service_plan 'modules/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: resourceGroup()
  params: {
    project:  project
    location: location
    skuName:  appServiceSkuName
    skuTier:  appServiceSkuTier
  }
}

module workflow_service_plan 'modules/logicappplan.bicep' = {
  name: 'workflowserviceplan'
  scope: resourceGroup()
  params: {
    project:  project
    location: location
    skuName:  workflowSkuName
    skuTier:  workflowSkuTier
  }
}

module logic_app 'modules/logicapp.bicep' = {
  name: 'logicapp'
  scope: resourceGroup()
  params: {
    storageAccountName:       storageAccountName
    storageKey:               storage_account.listKeys().keys[0].value
    storageFileShare:         storageFileShareName
    project:                  project
    location:                 location
    workflow_service_plan:    workflow_service_plan.outputs.workflowServicePlanId
    appInsInstrumentationKey: app_insights.outputs.appInsInstrumentationKey
    logicAppName:             logicAppName
    apiClientId:              apiClientId
  }
  dependsOn: [
    servicebus
  ]
}

module api_app 'modules/apiapp.bicep' = {
  name: 'webapi'
  scope: resourceGroup()
  params: {
    project:                project
    storageAccountName:     storageAccountName
    storageKey:             storage_account.listKeys().keys[0].value
    location:               location
    appServicePlanId:       app_service_plan.outputs.appServicePlanId
    kvName:                 kvname
    apiClientId:            apiClientId
    logicAppsUamiObjectId:  logic_app.outputs.logicAppPrincipalId
  }
  dependsOn: [
    servicebus
  ]
}

module ui_app 'modules/uiapp.bicep' = {
  name: 'webui'
  scope: resourceGroup()
  params: {
    project:          project
    location:         location
    appServicePlanId: app_service_plan.outputs.appServicePlanId
    uiClientId:       uiClientId
    apiClientId:      apiClientId
  }
  dependsOn: [
    app_insights
  ]
}

module app_insights 'modules/appinsights.bicep' = {
  name: 'appinsights'
  scope: resourceGroup()
  params: {
    location: location
    project:  project
  }
}


module servicebus 'modules/servicebus.bicep' = {
  name: 'servicebus'
  scope: resourceGroup()
  params: {
    project:  project
    location: location
    skuName:  serviceBusSkuName
  }
}

module logicapp_rbac_roles 'modules/logicapproles.bicep' = {
  name: 'logicRBACRoles'
  scope: resourceGroup()
  params: {
    uamiPrincipalId: logic_app.outputs.logicAppPrincipalId
  }
}

module apiapp_runtime_rbac_roles 'modules/apiroles_runtime.bicep' = {
  name: 'apiRuntimeRBACRoles'
  scope: resourceGroup()
  params: {
    uamiPrincipalId: api_app.outputs.principalId
  }
}
module apiapp_persistent_rbac_roles 'modules/apiroles_persistent.bicep' = {
  name: 'apiPersistentRBACRoles'
  scope: resourceGroup('icp-persistent')
  params: {
    uamiPrincipalId: api_app.outputs.principalId
  }
}
