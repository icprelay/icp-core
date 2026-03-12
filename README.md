# Integration Control Plane (ICP)

A cloud-native integration platform built on Azure that provides a centralized control plane for managing, monitoring, and executing integration workflows across multiple target systems.

## Overview

The Integration Control Plane (ICP) is an enterprise integration solution that enables organizations to orchestrate and manage complex integration scenarios. It combines a .NET API, Azure Logic Apps for workflow execution, and a web-based UI to provide a complete integration platform.

### Key Features

- **Centralized Integration Management**: Define and manage integration workflows, event types, and target systems from a single control plane
- **Multiple Integration Targets**: Support for various output targets
- **Event-Driven Architecture**: Process events asynchronously using Azure Service Bus and Logic Apps
- **Secure Credential Management**: Azure Key Vault integration for secure storage of connection strings and secrets
- **Run Tracking & Monitoring**: Comprehensive execution history with Azure Application Insights integration
- **Role-Based Access Control**: Azure AD authentication with separate policies for UI users and worker processes

## Architecture

### Components

```
┌─────────────────┐
│   Web UI        │  ASP.NET CORE (.NET 10)
│ (Azure App Svc) │  - Razor Pages
└────────┬────────┘  - Authentication Required
         │
         ▼
┌─────────────────┐
│   Web API       │  ASP.NET Core (.NET 10)
│ (Azure App Svc) │  - REST endpoints
└────────┬────────┘  - Authorization Required
         │           - Database access
         ▼
┌─────────────────┐
│   Logic Apps    │  Workflow Standard
│                 │  - Event processing
└────────┬────────┘  - Target execution
         │
         ▼
┌──────────────────────────────────────┐
│  Azure Resources                     │
│  • SQL Database (Runtime)            │
│  • Service Bus                       │
│  • Key Vault                         │
│  • Storage Account                   │
│  • Application Insights              │
└──────────────────────────────────────┘
```

### Project Structure

- **src/Icp.Api.Host** - ASP.NET Core Web API host
- **src/Icp.Contracts** - Shared contract definitions
- **src/Icp.Messaging** - Azure Service Bus Service 
- **src/Icp.Persistence** - Entity Framework Core data access layer with Runtime Db context
- **src/Icp.Secrets** - Azure Key Vault secret management
- **src/Icp.Storage** - Azure Blob Storage Service
- **src/Icp.Ui** - Web UI reference implementation
- **logicapps/icp-workspace/icp-logicapp** - Azure Logic Apps workflows
- **infra/** - Bicep infrastructure as code templates
  - `persistent_resources.bicep` - Long-lived resources (SQL, Storage, Key Vault, Managed Identities)
  - `runtime_resources.bicep` - Runtime-specific resources (App Services, Logic Apps)
  - `modules/` - Reusable Bicep modules


### Required Azure AD Groups

- `icp-sql-admins` - SQL database administrators

### Database Setup

The project uses one database:
- **Runtime Database**: Operational data for integration workflows

#### Apply Migrations

```bash
# Apply Runtime migrations
.\ApplyRuntimeMigration.cmd

```

#### Add Migrations

```bash
# Add Runtime migrations
.\AddRuntimeMigration.cmd <migration name>

```

## Deployment

### CI/CD Pipelines

The project includes Azure DevOps YAML pipelines:

- **deploy_api.yml** - Build and deploy Web API
- **deploy_ui.yml** - Build and deploy Web UI
- **deploy_logicapp.yml** - Deploy Logic Apps workflows
- **apply_migrations.yml** - Run database migrations
- **infra/deploy_persistent_resources.yml** - Deploy persistent infrastructure
- **infra/deploy_runtime_resources.yml** - Deploy runtime infrastructure
- **infra/cleanup_resources.yml** - Cleanup resources


## Authentication & Authorization

### Policies

- **UI Policy**: Requires `icp.access` scope for user-facing operations
- **Worker Policy**: Requires `icp.worker` role for backend processes
- **WorkerOrUI Policy**: Combined policy for endpoints accessible by both

### App Registrations

Two Entra ID app registrations are required:
1. **ICP API** - Protected Web API with exposed `icp.access` scope
2. **ICP UI** - Client application with delegated permissions to API

## Monitoring

- **Application Insights**: Telemetry, traces, and performance monitoring
- **Azure Monitor**: Resource health and metrics
- **SQL Database Auditing**: Database-level activity tracking
- **Run History**: Execution logs in the Runtime database

## Technology Stack

- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Cloud Platform**: Microsoft Azure
- **Authentication**: Microsoft Entra ID (Azure AD) / Auth0
- **Infrastructure**: Azure Bicep
- **Databases**: Azure SQL Database
- **Messaging**: Azure Service Bus
- **Workflows**: Azure Logic Apps (Standard)
- **Secrets Management**: Azure Key Vault
- **Monitoring**: Azure Application Insights

