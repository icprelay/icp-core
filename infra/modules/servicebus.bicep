param project string
param sbus_name string = '${project}-servicebus'
param location string = resourceGroup().location
param skuName string = 'Standard'

resource sbus_resource 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: sbus_name
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: { 
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    zoneRedundant: false
  }
}

resource sbus_RootManageSharedAccessKey 'Microsoft.ServiceBus/namespaces/authorizationrules@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: [
      'Listen'
      'Manage'
      'Send'
    ]
  }
}

resource sbus_default 'Microsoft.ServiceBus/namespaces/networkRuleSets@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'default'
  properties: {
    publicNetworkAccess: 'Enabled'
    defaultAction: 'Allow'
    virtualNetworkRules: []
    ipRules: []
  }
}

resource sbus_q_events 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'icp-events'
  properties: {
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT30M'
    maxDeliveryCount: 10
    lockDuration: 'PT2M'
    deadLetteringOnMessageExpiration: true
  }
}

resource sbus_q_process 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'icp-process'
  properties: {
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT30M'
    maxDeliveryCount: 10
    lockDuration: 'PT2M'
    deadLetteringOnMessageExpiration: true
  }
}

resource sbus_q_sinkblob 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'icp-targets-sink-blob'
  properties: {
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT30M'
    maxDeliveryCount: 10
    lockDuration: 'PT2M'
    deadLetteringOnMessageExpiration: true
  }
}

resource sbus_q_countblobs 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: sbus_resource
  name: 'icp-targets-sink-countblobs'
  properties: {
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT30M'
    maxDeliveryCount: 10
    lockDuration: 'PT2M'
    deadLetteringOnMessageExpiration: true
  }
}

output sbusId string = sbus_resource.id
output sbusName string = sbus_resource.name
