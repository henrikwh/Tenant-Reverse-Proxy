param  appConfigName string
param  keyVaultName string 

@allowed([
  'text/plain'
  'application/json'
  'application/xml'
  'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
])
param contentType string = 'application/json'
param value string
param keyName string
param managedIdentityWithAccessToAppConfiguration string

//If the value is 'NotASecret' then dont store in Key Vault
param   isSecrect  bool=  false

var conf = loadJsonContent('../config.json')
var roles = conf.roles

resource uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityWithAccessToAppConfiguration
}
resource appConfigurationStore 'Microsoft.AppConfiguration/configurationStores@2022-05-01' existing = {
  name: appConfigName
}

resource configStoreEntry 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' =  {
parent:appConfigurationStore
  name: keyName
  properties: {
    value: (!isSecrect) ?  value : '{"uri":"${keyVaultEntry.properties.secretUri}"}'
    contentType: (!isSecrect) ? contentType : 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
} 

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = if (isSecrect){
  name: keyVaultName
}

resource keyVaultEntry 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (isSecrect) {
  parent: keyVault
  name: replace(keyName, ':', '-')
  properties: {
    value: value
  }
}

resource managedIdentityCanReadNotificationSecret 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (isSecrect) {
  name: guid(roles['Key Vault Secrets User'], uami.id, keyVaultEntry.id)
  scope: keyVaultEntry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['Key Vault Secrets User'])
    principalId: uami.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

