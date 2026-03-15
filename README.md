# ICP Relay (icp-core)

ICP Relay is a cloud-native data delivery platform that enables SaaS vendors to deliver operational data to customer systems without building custom integrations.

The platform receives webhooks from a SaaS application and executes configurable integration workflows that deliver data to targets such as SMS Messaging, SQL databases, REST APIs, Microsoft Teams, SharePoint, or Power BI.

ICP consists of a .NET control plane API, Azure Logic Apps workflows for execution, and a lightweight UI for configuring and monitoring integration instances.


### Project Structure

- **src/Icp.Api.Host** - ASP.NET Core Web API host
- **src/Icp.Contracts** - Shared data contract definitions
- **src/Icp.Messaging** - Azure Service Bus Service 
- **src/Icp.Persistence** - Entity Framework Core data access layer with Runtime Db context
- **src/Icp.Secrets** - Azure Key Vault secret management
- **src/Icp.Storage** - Azure Blob Storage Service
- **src/Icp.Ui** - Web UI 
- **logicapps/icp-workspace/icp-logicapp** - Azure Logic Apps workflows

## Technology Stack

- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Cloud Platform**: Microsoft Azure
- **Authentication**: Microsoft Entra ID (Azure AD)
- **Infrastructure**: Azure Bicep
- **Databases**: Azure SQL Database
- **Messaging**: Azure Service Bus
- **Workflows**: Azure Logic Apps (Standard)
- **Secrets Management**: Azure Key Vault
- **Monitoring**: Azure Application Insights

