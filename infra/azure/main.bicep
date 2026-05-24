@description('Location for all resources')
param location string = resourceGroup().location

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('AES-256 encryption key (base64)')
@secure()
param encryptionKey string

@description('AES-256 IV (base64)')
@secure()
param encryptionIv string

@description('Basic Auth password for the API')
@secure()
param basicAuthPassword string

var prefix = 'gkassessment'

// Log Analytics
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${prefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

// Container Apps Environment
resource caEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${prefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Azure Container Registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: '${prefix}acr'
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: true }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
  name: '${prefix}-sql'
  location: location
  properties: { administratorLogin: 'sqladmin', administratorLoginPassword: sqlAdminPassword }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  parent: sqlServer
  name: 'GKAssessment'
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
}

// Redis Cache
resource redis 'Microsoft.Cache/redis@2023-04-01' = {
  name: '${prefix}-redis'
  location: location
  properties: { sku: { name: 'Basic', family: 'C', capacity: 0 }, enableNonSslPort: false }
}

// Key Vault
resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: '${prefix}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: true
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${prefix}-api'
  location: location
  properties: {
    managedEnvironmentId: caEnv.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'http' }
      registries: [{ server: acr.properties.loginServer, username: acr.listCredentials().username, passwordSecretRef: 'acr-password' }]
      secrets: [{ name: 'acr-password', value: acr.listCredentials().passwords[0].value }]
    }
    template: {
      containers: [{
        name: 'gk-api'
        image: '${acr.properties.loginServer}/gk-assessment:latest'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT',              value: 'Production' }
          { name: 'ASPNETCORE_URLS',                     value: 'http://+:8080' }
          { name: 'ConnectionStrings__DefaultConnection', value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=GKAssessment;User Id=sqladmin;Password=${sqlAdminPassword};TrustServerCertificate=True;' }
          { name: 'ConnectionStrings__Redis',             value: '${redis.properties.hostName}:6380,ssl=True,password=${redis.listKeys().primaryKey}' }
          { name: 'Encryption__Key',                     value: encryptionKey }
          { name: 'Encryption__IV',                      value: encryptionIv }
          { name: 'BasicAuth__Username',                  value: 'gkadmin' }
          { name: 'BasicAuth__Password',                  value: basicAuthPassword }
        ]
        resources: { cpu: json('0.5'), memory: '1Gi' }
      }]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

output apiUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output swaggerUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}/swagger'
