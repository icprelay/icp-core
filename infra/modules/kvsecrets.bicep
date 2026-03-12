param sqlConnectionStringName string
param sqlServerName string
param sqlDbName string
param kvname string

targetScope = 'resourceGroup'

resource kv 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: kvname

  resource connectionstring 'secrets' = {
    name: sqlConnectionStringName
    properties: {
      value: 'Server=tcp:${sqlServerName}.database.windows.net,1433;Database=${sqlDbName};Encrypt=True;Connection Timeout=30;Authentication=Active Directory Default;'
    }
  }
}

