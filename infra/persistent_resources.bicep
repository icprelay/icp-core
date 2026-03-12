// pipeline parameters  
param project               string
param adminObjectId         string // ICP SQL Admins group object ID
param adminLogin            string // ICP SQL Admins group display name

// computed parameters
param tenantId              string = subscription().tenantId
param location              string = resourceGroup().location
param storage_account_name  string = toLower(replace('${project}logicstorage', '-', ''))
param storage_sku_name      string = 'Standard_LRS'
param keyvault_name         string = '${project}-keyvault'
param sql_server_name       string = '${project}-sql'
param runtime_db_name       string = '${project}-runtime-db'
param web_api_uami_name     string = '${project}-web-api-uami'
param logic_app_uami_name   string = '${project}-logicapp-uami'


module keyvault 'modules/keyvault.bicep' = {
  name: keyvault_name
  scope: resourceGroup()
  params: {
    name:           keyvault_name
    location:       location
    tenantId:       tenantId
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sqldatabase'
  scope: resourceGroup()
  params: {
    dbName:           runtime_db_name
    sqlServerName:    sql_server_name
    location:         location
    adminObjectId:    adminObjectId
    adminLogin:       adminLogin
  }
}
  
module storage 'modules/storage.bicep' = {
  name: 'workflowstorage'
  scope: resourceGroup()
  params: {
    name:           storage_account_name
    location:       location
    skuName:        storage_sku_name
  }
}


resource web_api_uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: web_api_uami_name
  location: location
}

resource logic_app_uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: logic_app_uami_name
  location: location
}
