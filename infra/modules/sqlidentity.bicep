param sqlServerName string

param location string = resourceGroup().location

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
}
