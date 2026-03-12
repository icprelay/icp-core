param storageAccountName string
param storageKey string
param storageFileShare string
param project string
param location string = resourceGroup().location
param workflow_service_plan string
param appInsInstrumentationKey string
param apiClientId string
param uamiResourceGroup string = 'icp-persistent'
param logicAppName string

resource servicebus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: '${project}-servicebus'
}

resource uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing= {
  name: '${project}-logicapp-uami'
  scope: resourceGroup(uamiResourceGroup)
}

resource logic_app 'Microsoft.Web/sites@2024-11-01' = {
  name: logicAppName
  location: location
  kind: 'functionapp,workflowapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uami.id}': {}
    }
  }
  properties: {
    enabled: true
    serverFarmId: workflow_service_plan
  }
}

var appsettings = [
  {
    name: 'AzureWebJobsStorage'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageKey};EndpointSuffix=core.windows.net'
  }
  {
    name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageKey};EndpointSuffix=core.windows.net'
  }
  {
    name: 'WEBSITE_CONTENTSHARE'
    value: storageFileShare
  }
  {
    name: 'AzureWebJobsDashboard'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=core.windows.net'
  }
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: '~4'
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: 'dotnet'
  }
  {
    name: 'WEBSITE_RUN_FROM_PACKAGE'
    value: '0'
  }
  {
    name: 'APP_KIND'
    value: 'workflowApp'
  }
  {
    name: 'FUNCTIONS_WORKER_PROCESS_COUNT'
    value: '1'
  }
  {
    name: 'FUNCTIONS_INPROC_NET8_ENABLED'
    value: '1'
  }
  {
    name: 'AzureWebJobsFeatureFlags'
    value: 'EnableWorkerIndexing'
  }
  {
    name: 'AzureFunctionsJobHost__extensionBundle__id'
    value: 'Microsoft.Azure.Functions.ExtensionBundle.Workflows'
  }
  {
    name: 'LOGIC_APPS_POWERSHELL_VERSION'
    value: '7.4'
  }
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: appInsInstrumentationKey
  }
  {
    name: 'AZURE_CLIENT_ID'
    value: uami.properties.clientId
  }
  {
    name: 'AzureBlob_connectionString'
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageKey};EndpointSuffix=core.windows.net'
  }
  {
    name: 'serviceBus_connectionString'
    value: listKeys('${servicebus.id}/AuthorizationRules/RootManageSharedAccessKey', servicebus.apiVersion).primaryConnectionString
  }
  {
    name: 'ControlPlaneApiUrl'
    value: 'https://${project}-web-api.azurewebsites.net/api'
  }
  {
    name: 'LogicApp:UamiName'
    value: uami.name
  }
  {
    name: 'LogicApp:UamiIdentity'
    value: '/subscriptions/${subscription().subscriptionId}/resourcegroups/${uamiResourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/${uami.name}'
  }
  {
    name: 'ControlPlaneApiClientId'
    value: apiClientId
  }
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
]

resource logic_app_settings 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'appsettings'
  parent: logic_app
  properties: toObject(appsettings, arg => arg.name, arg => arg.value)
}

output logicAppPrincipalId string = uami.properties.principalId
