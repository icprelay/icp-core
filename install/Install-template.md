# Integration Control Plane Installation Guide

---

UI: https://icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui.azurewebsites.net

---

**Important**
This runbook is a starter template. Before proceeding, make a copy of this document and replace all placeholder values in the copied version as you go.

# How to replace placeholders efficiently (Azure DevOps editor):

1. Go to Edit view -> Contents tab
2. Place the cursor on a placeholder value and and click to select it (for example: REPLACE_ALL_WITH_YOUR_OWN_NAMING).
3. Hold or press CTRL + D repeatedly to select all occurrences of that value.
4. Paste (CTRL + V) your replacement to update all matches at once.


# Setup Stage



### 1. Choose a project name abbrevation, this must be 2-6 characters long. 

- In this document we refer to the project name as `REPLACE_ALL_WITH_YOUR_OWN_NAMING`.
- generate a guid for AppRoleId: `GENERATE_A_RANDOM_GUID_HERE_AND_REPLACE_ALL`

### 2. Create the following Entra ID security groups. 

- `icp-sql-admins`

After creation, add it to the table below and replace all placeholders

|Security Group|ObjectId|
|--|--|
|icp-sql-admins|`ICP_SQL_ADMINS_OBJECT_ID`|


### 3. Create a new Azure Subscription. Name it however you want.

### 4. Create a DevOps Service Connection to that Azure Subscription.

- Azure DevOps -> Project Settings -> Service connections -> New service connection -> Choose "Azure Resource Manager" -> Click [ **Next** ]
- In the following view you set the settings for the New Azure service connection. The correct settings are:

    |setting|value|
    |-------|-----|
    |Identity type|App registration (automatic)|
    |Credential|Workload identity federation|
    |Scope level|Subscription|
    |Subscription|\<your-new-subscription>| 
    |Resource group| _Leave blank_
    |Service Connection Name| `icp-service-connection` |
    |Service Management Reference (optional)|  | 
    |Description (optional)| 
    |Security| [x] Grant access permission to all pipelines |

- click [ **Save** ]
            
**Important note:**
Servce connection name is different in Azure DevOps and Azure Portal. To avoid confusion during deployment, it is recommended to rename it in Azure Portal after creation:
        
- Rename: 
1. click _Manage App registration_ 
2. Branding & properties 
3. _set name_ f.e. `icp-service-connection` 
4. click [ **Save** ]

### 5. Service Connection role assignment

1. Navigate to the new Subscription
2. Go to Access control (IAM)
3. +Add -> Add role assignment -> **Privileged administrator role**
4. Search for `User Access Administrator` and select it -> click [ **Next** ]
5. _Assign access to_ [x] **User, group, or service principal**
6. +Select members -> Search and choose `icp-service-connection` -> click [ **Select** ]
    - Naming convention is: `<organization>-<project>-<guid>` if not renamed.
7. click [ **Next** ]
8. _What user can do_ :  [x] **Allow user to assign all roles except privileged administrator roles Owner, UAA, RBAC (Recommended)**
9. click [ **Next** ]
10. click [ **Review + assign** ]

### 6. Service connection security group assignment

1. Navigate to Entra ID
2. Go to Groups -> Security Groups
3. Choose `icp-sql-admins` -> Members -> +Add members
4. Search for `icp-service-connection` and add to Group

### 7. Create App Registration for web **API**

1. Navigate to Entra ID → App registrations 
2. Click +New registration
3. Name it `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-api-app-reg` 
4. [x] **Accounts in this organizational directory only (Single tenant)**
5. click [ **Register** ]
6. Add the app registration **client id** the table below and replace all placeholders

    |property|value|
    |---|----|
    |Display name|icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-api-app-reg|
    |Application (client) ID|REPLACE_ALL_WITH_API_APP_REGISTRATION_ID|

### 8. Create App Registration for web **UI**


1. Navigate to Entra ID → App registrations 
2. Click +New registration
3. Name it `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-ui-app-reg`
4. [x] **Accounts in this organizational directory only (Single tenant)**
5. Set Redirect URI:


    |dropdown|redirect uri|
    |--|--|
    |Web|https://icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui.azurewebsites.net/signin-oidc|


6. click [ **Register** ]
7. Add the app registration **client id** the table below and replace all placeholders


    |property|value|
    |--|--|
    |Display name|icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-ui-app-reg|
    |Application (client) ID|REPLACE_ALL_WITH_UI_APP_REGISTRATION_ID|

# Deployment stage

## 1. Run "Deploy Persistent Resources"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|
|Azure subscription|`icp-service-connection`|
|icp-sql-admins group Object Id|`ICP_SQL_ADMINS_OBJECT_ID`|


After completion, assign sql managed identity to Directory Readers role.

1. Navigate to Entra ID -> Roles & admins
2. Search for `Directory Readers`
3. click -> [ ] **Directory Readers**
4. +Add assignments
5. Search `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-sql`
6. click [ **Add** ]

### 2. Run "Deploy Runtime Resources"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|
|Azure subscription|`icp-service-connection`|
|API App Registration Client ID|`REPLACE_ALL_WITH_API_APP_REGISTRATION_ID`|
|UI App Registration Client ID|`REPLACE_ALL_WITH_UI_APP_REGISTRATION_ID`|

### 3. Run "Deploy Logic Apps"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|
|Azure subscription|`icp-service-connection`|

### 4. Run "Deploy API"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|
|Azure subscription|`icp-service-connection`|

### 5. Run "Deploy UI"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|
|Azure subscription|`icp-service-connection`|

### 6. Run "DB Migrations + Grants + Seeding"

|parameter|value|
|--|--|
|Customer name|`REPLACE_ALL_WITH_YOUR_OWN_NAMING`|

## Authentication & Authorization Stage

### 1. Update API App Registration Manifest


1. Navigate to `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-api-app-reg` App registration
2. Click Manifest (under manage)

Either:

1. Edit directly in the editor (Recommended)
2. Download and open in text editor

Follow the instructions to modify the App Registration.

---

**identifierUris**

- replace:

`"identifierUris": [],`

- with:
```
"identifierUris": [
    "api://REPLACE_ALL_WITH_API_APP_REGISTRATION_ID"
],
```

---

**oauth2PermissionScopes**

- replace :

`"oauth2PermissionScopes": [],`

- with:

```
"oauth2PermissionScopes": [
    {
        "adminConsentDescription": "Allows the app to call ICP control plane API on behalf of the signed-in user.",
        "adminConsentDisplayName": "Access ICP API",
        "id": "30afe5b2-e3c4-44e1-b70e-bbf9c7f38ee8",
        "isEnabled": true,
        "type": "Admin",
        "userConsentDescription": null,
        "userConsentDisplayName": null,
        "value": "icp.access"
    }
],
```

---

**appRoles**

- replace:

`"appRoles": [],`

- with:

```
"appRoles": [
    {
        "allowedMemberTypes": [
            "Application"
        ],
        "description": "Allows access to LogicApps specific endpoints",
        "displayName": "icp.worker",
        "id": "GENERATE_A_RANDOM_GUID_HERE_AND_REPLACE_ALL",
        "isEnabled": true,
        "origin": "Application",
        "value": "icp.worker"
    }
],
```

---

**requiredResourceAccess**

- replace: 

```
"requiredResourceAccess": [
    {
        "resourceAppId": "00000003-0000-0000-c000-000000000000",
        "resourceAccess": [
            {
                "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                "type": "Scope"
            }
        ]
    }
],
```

- with:

```
"requiredResourceAccess": [
    {
        "resourceAppId": "00000003-0000-0000-c000-000000000000",
        "resourceAccess": [
            {
                "id": "37f7f235-527c-4136-accd-4a02d197296e",
                "type": "Scope"
            },
            {
                "id": "14dad69e-099b-42c9-810b-d002981feec1",
                "type": "Scope"
            },
            {
                "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                "type": "Scope"
            }
        ]
    }
],
```
---

- **Save** (or upload) the edited Manifest JSON

## 2. Update UI App Registration Manifest

1. Navigate to `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-ui-app-reg` App registration 
2. Click Manifest (under manage)

Either:

1. Edit directly in the editor (Recommended)
2. Download and open in text editor

Follow the instructions to modify the App Registration

---

**oauth2PermissionScopes**

- replace:

`"oauth2PermissionScopes": [],`

- with:

```
"oauth2PermissionScopes": [
    {
        "adminConsentDescription": "Allows the app to access the web API on behalf of the signed-in user",
        "adminConsentDisplayName": "Access the API on behalf of a user",
        "id": "549c39d8-89be-4740-ac4a-dba21e528806",
        "isEnabled": true,
        "type": "User",
        "userConsentDescription": "Allows this app to access the web API on your behalf",
        "userConsentDisplayName": "Access the API on your behalf",
        "value": "access_as_user"
    }
],
```

---

**requiredResourceAccess**

- replace:

```
"requiredResourceAccess": [
    {
        "resourceAppId": "00000003-0000-0000-c000-000000000000",
        "resourceAccess": [
            {
                "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                "type": "Scope"
            }
        ]
    }
],
```

- with:

```
"requiredResourceAccess": [
    {
        "resourceAppId": "REPLACE_ALL_WITH_API_APP_REGISTRATION_ID",
        "resourceAccess": [
            {
                "id": "30afe5b2-e3c4-44e1-b70e-bbf9c7f38ee8",
                "type": "Scope"
            }
        ]
    },
    {
        "resourceAppId": "00000003-0000-0000-c000-000000000000",
        "resourceAccess": [
            {
                "id": "37f7f235-527c-4136-accd-4a02d197296e",
                "type": "Scope"
            },
            {
                "id": "14dad69e-099b-42c9-810b-d002981feec1",
                "type": "Scope"
            },
            {
                "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                "type": "Scope"
            }
        ]
    }
],
```

---

**enableIdTokenIssuance**

- replace: 

```
"enableIdTokenIssuance": false
```

- with:

```
"enableIdTokenIssuance": true
```

---


- **Save** (or upload) the edited Manifest JSON

- Grant Admin consent for API Permission:

    1. Navigate to API permissions
    2. Click **Grant Admin Consent for \<organization>**

- Add a Client Secret for icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui app service

    1. Navigate to Certificates & secrets.
    2. Under Client secrets, click **+ New client secret**
    3. Description: `AzureAd:ClientSecret for icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui`
    4. Expires: 730d (or any)
    5. **copy the secret**
    6. Navigate to `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui`
    7. Environment variables (under Settings)
    8. **Paste** the secret to **AzureAd:ClientSecret** environment variable
    9. Click **Apply**, **Confirm**


## 3. Add AppRoleAssignment for LogicApp Managed identity

1. Find the following values

|resource|value|
|--|--|
|icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-logicapp-uami **object id**|LOGIC_APP_UAMI_OBJECT_ID   |
|icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-api-app-reg **!! service principal !! object id !!**| API_APP_REGISTRATION_SERVICE_PRINCIPAL_OBJECT_ID  |

2. Go to Azure Cloud Shell in the Portal (use PowerShell)

3. Execute following commands to assign `icp.worker` role to logic app managed identity (icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-logicapp-uami).

```

Connect-MgGraph -Scopes "AppRoleAssignment.ReadWrite.All"                   

```
- You are now prompted to authenticate to Microsoft Graph. Follow the instructions in the console to sign in.
- After you are welcomed to Microsoft Graph, enter the following commands:

```
$params = @{
	principalId = "LOGIC_APP_UAMI_OBJECT_ID"
	resourceId = "API_APP_REGISTRATION_SERVICE_PRINCIPAL_OBJECT_ID"
	appRoleId = "GENERATE_A_RANDOM_GUID_HERE_AND_REPLACE_ALL"
}
```

```
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId LOGIC_APP_UAMI_OBJECT_ID -BodyParameter $params
```

# Smoke testing stage

## 1. Create a new Integration instance

1. Navigate to the UI - https://icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-web-ui.azurewebsites.net
2. Sign in
3. Navigate to Integration instances
4. Create new with following values:

|field|value|
|--|--|
|subscribed event type|smoke-test-event|
|integration target|smoke-test-target|
|enabled|[x] enabled|
|parameters|`parameters1=value1`|

5. **Save**

## 2. Set Secret value

1. Find the **Secrets (!)** button from the integration instance list
2. Set a value for the secret 
```
secret1=secretvalue
```
3. **Save**

## 3. Enable and disable instance

**Important!** Make sure to leave it in enabled state.

## 4. Find the Logic App invoke URL

1. Navigate to Azure Portal
2. Search for `icp-REPLACE_ALL_WITH_YOUR_OWN_NAMING-logicapp`
3. Go to Workflows (in sidebar)
4. Open **ingest**
5. In the top right, find settings
6. Click **Info**
7. Copy the trigger Url

## 5. Send a HTTP POST request with POSTMAN 

1. Open Postman
2. Create a new request
3. set the HTTP method to POST
4. paste the Logic App trigger url in the the request URL field

**Configure headers**

5. Go to the Headers tab
6. Add the following header:

|key|value|
|--|--|
|Content-Type|application/json|

**Configure body**

7. Go to the Body tab
8. Select raw
9. Select JSON as the format
10. Paste the JSON payload as body:

```
  {
    "data": {
      "id":"1"
    },
    "meta":{
      "type":"core-smoke"
    }
  }
```

11. Click **Send**
12. Verify the response
    - HTTP status `202 Accepted`
    - Body is empty

If you do not receive `202 Accepted`, the request did not reach the workflow correctly. Common causes are:

- Incorrect trigger URL
- Invalid JSON (syntax or schema)
- Missing or incorrect headers

## 6. Verify Integration instance state

- The integration instance status should be `Succeeded` and the timestamp values are shown.