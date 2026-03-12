param name string
param location string
param tenantId string

resource keyvault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: name
  location: location
  properties: {
    tenantId: tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
    accessPolicies: []
    sku: {
      name: 'standard'
      family: 'A'
    }
  }
}
