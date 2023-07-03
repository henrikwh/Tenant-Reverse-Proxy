@description('SKU size')
@allowed([
  'S1'
  'F1'
])
param skuSize string = 'S1'

param location string

@secure()
param secret string

var names = loadJsonContent('names.json')
var conf = loadJsonContent('config.json') 
var roles = conf.roles

var baseName = substring(uniqueString(resourceGroup().id), 0, 4)


var environments = [
  'Development'
  'Test'
  'Production'
]

@description('Specifies the names of the key-value resources. The name is a combination of key and label with $ as delimiter. The label is optional.')
param keyValueNames array = [
  'API:Settings'
  'API:Settings$Development'
  'API:Settings$Production'
  'ReverseProxy:Settings'
  'ReverseProxy:Settings$Development'
  'ReverseProxy:Settings$Production'
  'TenantDirectory:Tenants$Development'
  'TenantDirectory:Tenants$Production'
  'Generel'

]

@description('Array holding settings to be loaded into app confiuration')
param keyValueValues array = [
  loadTextContent('conf/API_Settings.json')
  loadTextContent('conf/API_Settings_Development.json')
  loadTextContent('conf/API_Settings_Production.json')
  loadTextContent('conf/ReverseProxy_Settings.json')
  loadTextContent('conf/ReverseProxy_Settings_Development.json')
  loadTextContent('conf/ReverseProxy_Settings_Production.json')
  loadTextContent('conf/TenantDirectory_Tenants_Development.json')
  loadTextContent('conf/TenantDirectory_Tenants_Production.json')
  loadTextContent('conf/Global_Settings.json')
]
//============================= User assigned managed identity  =============================
resource webappUamis 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${names.uamis.appconfigDemo}-${baseName}'
  location: location
}

resource controlPlaneUamis 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'controlPlane-${baseName}'
  location: location
}


//============================= Web app  =============================
resource serverFarm 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: names.website.appServiceplan
  location: location
  sku: {
    size: skuSize
    name: skuSize
    capacity: 1
  }
  kind: 'windows'

}

resource proxyApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${names.website.webapp}-${baseName}'
  location: location
  kind: 'app'
  properties: {

    serverFarmId: serverFarm.id
    clientAffinityEnabled: false
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${webappUamis.id}': {} }
  }
  resource appsettings 'config@2022-09-01' = {
    name: 'appsettings'
    properties: {
      'ApplicationInsights:ConnectionString': applicationInsights.properties.ConnectionString
      KeyVaultName: keyVault.name
      ASPNETCORE_ENVIRONMENT: 'Production'
      'AppConfiguration:UserAssignedManagedIdentityClientId': webappUamis.properties.clientId
      'ChangeSubscription:UserAssignedManagedIdentityClientId': webappUamis.properties.clientId
      'AppConfiguration:Uri': configurationStore.properties.endpoint
      'ChangeSubscription:ServiceBusTopic': serviceBusTopicForChangeNotification.name
      'ChangeSubscription:ServiceBusNamespace': serviceBusNamespace.properties.serviceBusEndpoint
    }
  }

  resource web 'config' = {
    name: 'web'
    properties: {

      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
      loadBalancing:  'PerSiteRoundRobin'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled' 
    }

  }
}


resource managementService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'managementService-${baseName}'
  location: location
  kind: 'app'
  properties: {

    serverFarmId: serverFarm.id
    clientAffinityEnabled: false
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${controlPlaneUamis.id}': {} }
  }
  resource appsettings 'config@2022-09-01' = {
    name: 'appsettings'
    properties: {
      'ApplicationInsights:ConnectionString': applicationInsights.properties.ConnectionString
      KeyVaultName: keyVault.name
      ASPNETCORE_ENVIRONMENT: 'Production'
      'AppConfiguration:UserAssignedManagedIdentityClientId': controlPlaneUamis.properties.clientId
      'ChangeSubscription:UserAssignedManagedIdentityClientId': controlPlaneUamis.properties.clientId
      'AppConfiguration:Uri': configurationStore.properties.endpoint
      'ChangeSubscription:ServiceBusTopic': serviceBusTopicForChangeNotification.name
      'ChangeSubscription:ServiceBusNamespace': serviceBusNamespace.properties.serviceBusEndpoint
    }
  }

  resource web 'config' = {
    name: 'web'
    properties: {

      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
      loadBalancing:  'PerSiteRoundRobin'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled' 
    }

  }
}




resource webAppWeatherReport 'Microsoft.Web/sites@2022-03-01' = {
  name: '${names.website.webapp}-weather-${baseName}'
  location: location
  kind: 'app'
  properties: {

    serverFarmId: serverFarm.id
    clientAffinityEnabled: false
    /*siteConfig: {
      netFrameworkVersion: 'v6.0'
    }*/

  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${webappUamis.id}': {} }
  }
  resource appsettings 'config@2022-03-01' = {
    name: 'appsettings'
    properties: {
      'ApplicationInsights:ConnectionString': applicationInsights.properties.ConnectionString
      KeyVaultName: keyVault.name
      ASPNETCORE_ENVIRONMENT: 'Production'
      'AppConfiguration:UserAssignedManagedIdentityClientId': webappUamis.properties.clientId
      'AppConfiguration:Uri': configurationStore.properties.endpoint
      'ChangeSubscription:ServiceBusTopic': serviceBusTopicForChangeNotification.name
      'ChangeSubscription:ServiceBusNamespace': serviceBusNamespace.properties.serviceBusEndpoint
      'ChangeSubscription:UserAssignedManagedIdentityClientId': webappUamis.properties.clientId
    }
  }

  resource web 'config' = {
    name: 'web'
    properties: {

      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
      loadBalancing:  'PerSiteRoundRobin'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled' 
    }

  }
}



//============================= App Configuration =============================

resource configurationStore 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  location: location
  name: '${names.appConfiguration.name}-${baseName}'
  sku: {
    name: 'standard'
  }
  properties: {

  }
}




module AddEnvironmentSettings 'modules/addAppConfiguration.bicep' = [for (item, i) in keyValueNames: {
  name: replace(replace('Adding-${item}',':',''),'$','')
  params: {
    keyName: item
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value: keyValueValues[i]
    appConfigName : configurationStore.name
    isSecrect: false
    keyVaultName: keyVault.name 
  }
}]


module JwtKeyAdd 'modules/addAppConfiguration.bicep' =  {
  name: 'JwtKey'
  params: {
    keyName: 'Jwt:Secret'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:  secret
    appConfigName : configurationStore.name
    keyVaultName: keyVault.name 
    isSecrect: true
    
  }
}
module ChangeSubscriptionUserAssignedManagedIdentityClientId 'modules/addAppConfiguration.bicep' =  {
  name: 'ChangeSubscriptionUserAssignedManagedIdentityClientId'
  params: {
    keyName: 'ChangeSubscription:UserAssignedManagedIdentityClientId'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:   webappUamis.properties.clientId
    appConfigName : configurationStore.name
    contentType: 'text/plain'
    isSecrect:false
    keyVaultName: keyVault.name 
  }
}

module AppConfigurationUserAssignedManagedIdentityClientId 'modules/addAppConfiguration.bicep' =  {
  name: 'AppConfigurationUserAssignedManagedIdentityClientId'
  params: {
    keyName: 'AppConfiguration:UserAssignedManagedIdentityClientId'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:   webappUamis.properties.clientId
    appConfigName : configurationStore.name
    contentType: 'text/plain'
    isSecrect:false
    keyVaultName: keyVault.name 
  }
}


module AzureAdClientId 'modules/addAppConfiguration.bicep' =  {
  name: 'AzureAdClientId'
  params: {
    keyName: 'AzureAd:ClientId'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:   conf.Proxy.Aad.ClientId
    appConfigName : configurationStore.name
    contentType: 'text/plain'
    isSecrect:false
    keyVaultName: keyVault.name 
  }
}



module  ServiceBusNamespace 'modules/addAppConfiguration.bicep' =  {
  name: 'ServiceBusNamespace'
  params: {
    keyName: 'ChangeSubscription:ServiceBusNamespace'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:   serviceBusNamespace.name
    appConfigName : configurationStore.name
    contentType: 'text/plain'
    isSecrect:false
    keyVaultName: keyVault.name 
  }
}


module  ServiceTopic 'modules/addAppConfiguration.bicep' =  {
  name: 'ServiceTopc'
  params: {
    keyName: 'ChangeSubscription:ServiceBusTopic'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value:   serviceBusTopicForChangeNotification.name
    appConfigName : configurationStore.name
    contentType: 'text/plain'
    isSecrect:false
    keyVaultName: keyVault.name 
  }
}


resource featureFlagDoMagic 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = [for env in environments: {
  name: '.appconfig.featureflag~2F${names.features.magic.name}$${env}'
  parent: configurationStore
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
    tags: {}
    value: '{"id": "${names.features.magic.name}", "description": "", "enabled": ${names.features.magic.default}, "conditions": {"client_filters":[]}}'
  }
}]

resource featureFlagShowDebugView 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = [for env in environments: {
  name: '.appconfig.featureflag~2F${names.features.showDebugView.name}$${env}'
  parent: configurationStore
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
    tags: {}
    value: '{"id": "${names.features.showDebugView.name}", "description": "", "enabled": ${names.features.showDebugView.default}, "conditions": {"client_filters":[]}}'
  }
}]

resource featureFlagProcessKeyVaultChangeEvents 'Microsoft.AppConfiguration/configurationStores/keyValues@2022-05-01' = [for env in environments: {
  name: '.appconfig.featureflag~2F${names.features.autoUpdateLatestVersionSecrets.name}$${env}'
  parent: configurationStore
  properties: {
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
    tags: {}   
    value: '{"id": "${names.features.autoUpdateLatestVersionSecrets.name}", "description": "", "enabled": ${names.features.autoUpdateLatestVersionSecrets.default}, "conditions": {"client_filters":[]}}'
  }
}]

//============================= EventGrid and Service bus =============================

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: '${names.serviceBus.nameSpace}-${baseName}'
  location: location
  sku: {
    name: 'Standard'
  }
}

resource serviceBusTopicForChangeNotification 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  name: 'sb-appconfigurationChangeTopic'
  parent: serviceBusNamespace
  properties: {
  }
}

// resource serviceBusTopicForProxyChangeNotification 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
//   name: 'sb-proxyChangeTopic'
//   parent: serviceBusNamespace
//   properties: {
//   }
// }

resource eventGridSystemTopicForConfigurationStore 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: 'eg-systemChangeTopic'
  location: location
  properties: {
    source: configurationStore.id
    topicType: 'Microsoft.AppConfiguration.ConfigurationStores'
  }
}

resource eventGridSystemTopicForKeyVault 'Microsoft.EventGrid/systemTopics@2022-06-15' = {
  name: 'eg-keyVaultSystemChangeTopic'
  location: location
  properties: {
    source: keyVault.id
    topicType: 'Microsoft.KeyVault.Vaults'
  }
}

resource changeEventSubscriptionac 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = {
  name: 'changeSubscription-kv'
  parent: eventGridSystemTopicForKeyVault
  properties: {
    destination: {
      endpointType: 'ServiceBusTopic'
      properties: {
        resourceId: serviceBusTopicForChangeNotification.id
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.KeyVault.KeyNewVersionCreated'
        'Microsoft.KeyVault.KeyNearExpiry'
        'Microsoft.KeyVault.KeyExpired'
        'Microsoft.KeyVault.SecretNewVersionCreated'
        'Microsoft.KeyVault.SecretNearExpiry'
        'Microsoft.KeyVault.SecretExpired'
        // 'Microsoft.KeyVault.CertificateNewVersionCreated'
        // 'Microsoft.KeyVault.CertificateNearExpiry'
        // 'Microsoft.KeyVault.CertificateExpired'
      ]
    }
    eventDeliverySchema: 'EventGridSchema'
  }
}

resource changeEventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2022-06-15' = {
  name: 'changeSubscription'
  parent: eventGridSystemTopicForConfigurationStore
  properties: {
    destination: {
      endpointType: 'ServiceBusTopic'
      properties: {
        resourceId: serviceBusTopicForChangeNotification.id
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.AppConfiguration.KeyValueModified'
      ]
    }
    eventDeliverySchema: 'EventGridSchema'
  }
}

//============================= Key Vault and secrect =============================
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${names.keyVault.name}-${baseName}'
  location: location
  tags: {
    
  }

  properties: {
    createMode: 'default'
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    enableRbacAuthorization: true
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 7
    tenantId: subscription().tenantId
  }
}



module AppInstighsConnectionStringSecret 'modules/addAppConfiguration.bicep' = {
  name: 'StoreAppInsightsConnections'
  params: {
    keyName: 'API:Settings:Secrets:ConnectionStrings:AppInsights'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value: applicationInsights.properties.ConnectionString
    appConfigName: configurationStore.name
    keyVaultName: keyVault.name
    isSecrect: false
    contentType: 'text/plain'
  }
}

module ServiceBusConnectionStringSecret 'modules/addAppConfiguration.bicep' = {
  name: 'ServiceBusConnectionStringSecret'
  params: {
    keyName: 'API:Settings:Secrets:ConnectionStrings:ServiceBus'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value: serviceBusConnectionString
    appConfigName: configurationStore.name
    keyVaultName: keyVault.name
    isSecrect: false
contentType: 'text/plain'

  }
}

module AppConfigurationConnectionStringSecret 'modules/addAppConfiguration.bicep' = {
  name: 'AppConfigurationConnectionStringSecret'
  params: {
    keyName: 'API:Settings:Secrets:ConnectionStrings:AppConfig'
    managedIdentityWithAccessToAppConfiguration: webappUamis.name
    value: appConfigReadonlyConnectionString.connectionString
    appConfigName: configurationStore.name
    keyVaultName: keyVault.name
    isSecrect: false
    contentType: 'text/plain'
  }
}

module AppConfigurationWriteConnectionStringSecret 'modules/addAppConfiguration.bicep' = {
  name: 'AppConfigurationWroteConnectionStringSecret'
  params: {
    keyName: 'TenantManagement:Settings:Secrets:ConnectionStrings:AppConfig'
    managedIdentityWithAccessToAppConfiguration: controlPlaneUamis.name
    value: appConfigReadWriteConnectionString.connectionString
    appConfigName: configurationStore.name
    keyVaultName: keyVault.name
  }
}

//============================= Role assignments =============================

resource managedIdentityCanReadConfigurationStore 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['App Configuration Data Reader'], webappUamis.id)
  scope: configurationStore
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['App Configuration Data Reader'])
    principalId: webappUamis.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource managedIdentityCanWriteReadConfigurationStore 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['App Configuration Data Owner'], controlPlaneUamis.id)
  scope: configurationStore
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['App Configuration Data Owner'])
    principalId: controlPlaneUamis.properties.principalId
    principalType: 'ServicePrincipal'
  }
}



resource managedIdentityReadNotification 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, roles['Azure Service Bus Data Receiver'], webappUamis.id)
  scope: serviceBusTopicForChangeNotification
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roles['Azure Service Bus Data Receiver'])
    principalId: webappUamis.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource managedIdentityCreateSubscription 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, roles.Contributor, webappUamis.id)
  scope: serviceBusTopicForChangeNotification
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roles.Contributor)
    principalId: webappUamis.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

//============================= Log analytics =============================
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${names.monitor.appinsights}-${baseName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    //DisableIpMasking: false
    //DisableLocalAuth: false
    //Flow_Type: 'Bluefield'
    //ForceCustomerStorageForProfiler: false
    //publicNetworkAccessForIngestion: 'Enabled'
    //publicNetworkAccessForQuery: 'Enabled'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

param sku string = 'PerGB2018'
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${names.monitor.logAnalyticsWorkspace}-${baseName}'
  location: location
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}






var serviceBusEndpoint = '${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey'
var serviceBusConnectionString = listKeys(serviceBusEndpoint, serviceBusNamespace.apiVersion).primaryConnectionString


var appConfigReadonlyConnectionString = filter(configurationStore.listKeys().value, k => k.name == 'Primary Read Only')[0]
var appConfigReadWriteConnectionString = filter(configurationStore.listKeys().value, k => k.name == 'Primary')[0]
output applicationInsights_ConnectionString string = applicationInsights.properties.ConnectionString
output changeSubscription_ServiceBusConnectionString string = serviceBusConnectionString
output connectionStrings_AppConfig string = appConfigReadonlyConnectionString.connectionString
output proxyEndpoint string = proxyApp.properties.defaultHostName
output webappWeatherEndpoint string = webAppWeatherReport.properties.defaultHostName
output managementServiceEndpoint string = managementService.properties.defaultHostName



//Devevlopment settings below. Used for running locally
output appConfigReadWriteConnectionString string = appConfigReadWriteConnectionString.connectionString
output appConfigReadConnectionString string = appConfigReadonlyConnectionString.connectionString
output appConfigurationName string =  configurationStore.name
output appConfigurationEndpoint string =  configurationStore.properties.endpoint

