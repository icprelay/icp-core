param storageAccountName   string
param storageFileShareName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
}


resource file_services 'Microsoft.Storage/storageAccounts/fileServices@2025-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    protocolSettings: {
      smb: {}
    }
    cors: {
      corsRules: []
    }
    shareDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource storage_file_share 'Microsoft.Storage/storageAccounts/fileServices/shares@2025-01-01' = {
  parent: file_services
  name: storageFileShareName
  properties: {
    shareQuota: 2
    enabledProtocols: 'SMB'
  }
}
