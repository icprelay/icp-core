param uamiPrincipalId string

resource servicebusdatasenderrole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(uamiPrincipalId, 'servicebus-data-sender')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39' // Service Bus Data Sender
    )
    principalId: uamiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource secrets_user 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(uamiPrincipalId, 'secrets-user')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
    )
    principalId: uamiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

