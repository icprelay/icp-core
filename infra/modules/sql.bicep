param sqlServerName string
param dbName string
param adminObjectId string
param adminLogin string

param location string = resourceGroup().location

@secure()
param nonce string = newGuid()

// do not run if existing server found
resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
    administratorLogin: 'sqladminuser'
    administratorLoginPassword: nonce //this is disabled in favor of AAD auth 
  }
}

resource sqlAadAdmin 'Microsoft.Sql/servers/administrators@2023-08-01' = {
  name: 'ActiveDirectory'
  parent: sqlServer
  properties: {
    administratorType: 'ActiveDirectory'
    sid: adminObjectId
    login: adminLogin
    tenantId: subscription().tenantId
  }
}

resource sqlAadOnly 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-08-01' = {
  name: 'Default'
  parent: sqlServer
  properties: {
    azureADOnlyAuthentication: true
  }
  dependsOn: [
    sqlAadAdmin
  ]
}

resource firewallRules 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllInternalAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource runtimeDb 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: dbName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    requestedBackupStorageRedundancy: 'Zone'
    maxSizeBytes: 53687091200 // 50 GB  
  }
}

output ServerName string = sqlServerName
