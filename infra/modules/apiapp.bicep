param project string
param storageAccountName string
param storageKey string
param appServicePlanId string
param location string
param kvName string
param apiClientId string
param logicAppsUamiObjectId string

resource servicebus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: '${project}-servicebus'
}

resource appIns 'Microsoft.Insights/components@2020-02-02' existing = {
  name: '${project}-appins'
  scope: resourceGroup()
}

resource uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: '${project}-web-api-uami'
  scope: resourceGroup('icp-persistent')
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: '${project}-web-api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uami.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appIns.properties.InstrumentationKey
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appIns.properties.InstrumentationKey};IngestionEndpoint=https://${appIns.location}-1.in.applicationinsights.azure.com/;LiveEndpoint=https://${appIns.location}.livediagnostics.monitor.azure.com'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: uami.properties.clientId
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'InstrumentationEngine_EXTENSION_VERSION'
          value: '~1'
        }
        {
          name: 'SnapshotDebugger_EXTENSION_VERSION'
          value: '~1'
        }
        {
          name: 'Keyvault:VaultUri'
          value: 'https://${kvName}.vault.azure.net'
        }
        {
          name: 'LogicApps:UamiObjectId'
          value: logicAppsUamiObjectId
        }
        {
          name: 'AzureAd:TenantId'
          value: subscription().tenantId
        }
        {
          name: 'AzureAd:ClientId'
          value: apiClientId
        }
        {
          name: 'ServiceBus:Domain'
          value: 'https://${servicebus.name}.servicebus.windows.net'
        }
        {
          name: 'Storage:Domain'
          value: 'https://${storageAccountName}.blob.core.windows.net'
        }
      ]
      connectionStrings: [
        {
          name: 'RuntimeDb'
          connectionString: 'Server=tcp:${project}-sql.database.windows.net,1433;Database=${project}-runtime-db;Encrypt=True;Connection Timeout=30;Authentication=Active Directory Default;'
          type: 'SQLAzure'
        }
      ]
    }
  }
}

output appServiceId string = appService.id
output principalId string = uami.properties.principalId
output apiAppName string = appService.name
