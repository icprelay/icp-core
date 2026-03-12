# Logic app project

## Getting started

1. copy `icp-workspace.code-workspace.template`
2. rename it to `icp-workspace.code-workspace`

## Requirements

- Azure Service Bus
- Azure Storage Account
  - blob storage
  - table storage
- Azure Key Vault (optional)

Run `infra/deploy_development_resources.yml` pipeline to deploy these resources

## HTTP authentication

Run `Switch-WorkflowAuth.ps1` to toggle between managed identity / local HTTP authentication. Toggle between `local` or `production`

## local.settings.json

Add a new file to `logicapps/icp-workspace/icp-logicapp` folder called `local.settings.json` with the following contents:

Fill in the empty values

```
{
  "IsEncrypted": false,
  "Values": {
    "APP_KIND": "workflowapp",
    "ProjectDirectoryPath": "<FILL>\\logicapps\\icp-workspace\\icp-logicapp",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_INPROC_NET8_ENABLED": "1",
    "AzureBlob_connectionString": "",
    "azureTables_connectionString": "",
    "serviceBus_connectionString": "",
    "ControlPlaneApiUrl": "",
    "ControlPlaneApiUamiName": "<not needed for local development>",
    "ControlPlaneApiClientId": "<not needed for local development>",
    "LogicApp:UamiIdentity": "<not needed for local development>",
    "LogicApp:UamiName": "<not needed for local development>"
  }
}
```
