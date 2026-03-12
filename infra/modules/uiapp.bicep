param project string
param appServicePlanId string
param location string
param uiClientId string
param apiClientId string

resource appIns 'Microsoft.Insights/components@2020-02-02' existing = {
  name: '${project}-appins'
  scope: resourceGroup()
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: '${project}-web-ui'
  location: location
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appIns.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appIns.properties.InstrumentationKey};IngestionEndpoint=https://${appIns.location}-1.in.applicationinsights.azure.com/;LiveEndpoint=https://${appIns.location}.livediagnostics.monitor.azure.com'
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
          name: 'IcpApi:BaseUrl'
          value: 'https://${project}-web-api.azurewebsites.net'
        }
        {
          name: 'IcpApi:Scope'
          value: 'api://${apiClientId}/icp.access'
        }
        {
          name: 'AzureAd:TenantId'
          value: subscription().tenantId
        }
        {
          name: 'AzureAd:ClientId'
          value: uiClientId
        }
        {
          name: 'AzureAd:ClientSecret'
          value: ''
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

output appServiceId string = appService.id
