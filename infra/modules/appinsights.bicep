@description('Location for Application Insights')
param location string = resourceGroup().location
param project string
param applicationInsightsName string = '${project}-appins'
param lawsName string = '${project}-law'


resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: lawsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

output appInsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
