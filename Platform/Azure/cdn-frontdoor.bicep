targetScope = 'resourceGroup'

@description('Front Door profile name')
param profileName string = 'afd-usm-prod'

@description('Front Door endpoint name')
param endpointName string = 'inventory-edge'

@description('Front Door origin hostname (AKS ingress public DNS)')
param originHostName string

@description('Custom domain host name')
param customDomainHostName string = 'inventory.usm.example.com'

@description('WAF policy mode')
@allowed([
  'Prevention'
  'Detection'
])
param wafMode string = 'Prevention'

resource profile 'Microsoft.Cdn/profiles@2024-02-01' = {
  name: profileName
  location: 'global'
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
}

resource endpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = {
  name: endpointName
  parent: profile
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource wafPolicy 'Microsoft.Network/frontDoorWebApplicationFirewallPolicies@2023-05-01' = {
  name: '${profileName}-waf'
  location: 'global'
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: wafMode
      requestBodyCheck: 'Enabled'
      maxRequestBodySizeInKb: 128
      fileUploadLimitInMb: 100
    }
    managedRules: {
      managedRuleSets: [
        {
          ruleSetType: 'Microsoft_DefaultRuleSet'
          ruleSetVersion: '2.1'
        }
        {
          ruleSetType: 'Microsoft_BotManagerRuleSet'
          ruleSetVersion: '1.0'
        }
      ]
    }
    customRules: {
      rules: [
        {
          name: 'GlobalRateLimit'
          enabledState: 'Enabled'
          priority: 1
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 1000
          action: 'Block'
          ruleType: 'RateLimitRule'
          matchConditions: [
            {
              matchVariable: 'RemoteAddr'
              operator: 'IPMatch'
              negateCondition: false
              matchValue: [
                '0.0.0.0/0'
              ]
            }
          ]
        }
      ]
    }
  }
}

resource originGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = {
  name: 'usm-origin-group'
  parent: profile
  properties: {
    sessionAffinityState: 'Disabled'
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 0
    }
    healthProbeSettings: {
      probePath: '/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 120
    }
  }
}

resource origin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = {
  name: 'usm-aks-origin'
  parent: originGroup
  properties: {
    hostName: originHostName
    httpPort: 80
    httpsPort: 443
    originHostHeader: originHostName
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

resource customDomain 'Microsoft.Cdn/profiles/customDomains@2024-02-01' = {
  name: 'inventory-custom-domain'
  parent: profile
  properties: {
    hostName: customDomainHostName
    tlsSettings: {
      certificateType: 'ManagedCertificate'
      minimumTlsVersion: 'TLS12'
    }
  }
}

resource route 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = {
  name: 'inventory-route'
  parent: endpoint
  properties: {
    originGroup: {
      id: originGroup.id
    }
    origins: [
      {
        id: origin.id
      }
    ]
    customDomains: [
      {
        id: customDomain.id
      }
    ]
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
    linkToDefaultDomain: 'Disabled'
    cacheConfiguration: {
      queryStringCachingBehavior: 'IgnoreSpecifiedQueryStrings'
      queryParameters: 'utm_source,utm_campaign'
      compressionSettings: {
        isCompressionEnabled: true
        contentTypesToCompress: [
          'text/html'
          'text/css'
          'text/javascript'
          'application/javascript'
          'application/json'
          'application/octet-stream'
          'application/wasm'
        ]
      }
    }
    ruleSets: [
      {
        id: staticAssetRuleSet.id
      }
    ]
  }
}

resource staticAssetRuleSet 'Microsoft.Cdn/profiles/ruleSets@2024-02-01' = {
  name: 'static-assets'
  parent: profile
}

resource staticAssetCachingRule 'Microsoft.Cdn/profiles/ruleSets/rules@2024-02-01' = {
  name: 'cache-angular-assets'
  parent: staticAssetRuleSet
  properties: {
    order: 1
    conditions: [
      {
        name: 'UrlFileExtension'
        parameters: {
          operator: 'Equal'
          negateCondition: false
          typeName: 'DeliveryRuleUrlFileExtensionConditionParameters'
          matchValues: [
            'js'
            'css'
            'woff2'
            'png'
            'jpg'
            'svg'
          ]
          transforms: []
        }
      }
    ]
    actions: [
      {
        name: 'CacheExpiration'
        parameters: {
          typeName: 'DeliveryRuleCacheExpirationActionParameters'
          cacheBehavior: 'Override'
          cacheDuration: '365.00:00:00'
        }
      }
    ]
    matchProcessingBehavior: 'Stop'
  }
}

resource mediaCachingRule 'Microsoft.Cdn/profiles/ruleSets/rules@2024-02-01' = {
  name: 'cache-api-media'
  parent: staticAssetRuleSet
  properties: {
    order: 2
    conditions: [
      {
        name: 'UrlPath'
        parameters: {
          operator: 'BeginsWith'
          negateCondition: false
          typeName: 'DeliveryRuleUrlPathMatchConditionParameters'
          matchValues: [
            '/api/v1/media'
            '/api/v2/media'
          ]
          transforms: []
        }
      }
    ]
    actions: [
      {
        name: 'CacheExpiration'
        parameters: {
          typeName: 'DeliveryRuleCacheExpirationActionParameters'
          cacheBehavior: 'Override'
          cacheDuration: '30.00:00:00'
        }
      }
    ]
    matchProcessingBehavior: 'Continue'
  }
}

resource securityPolicy 'Microsoft.Cdn/profiles/securityPolicies@2024-02-01' = {
  name: 'waf-association'
  parent: profile
  properties: {
    parameters: {
      type: 'WebApplicationFirewall'
      wafPolicy: {
        id: wafPolicy.id
      }
      associations: [
        {
          domains: [
            {
              id: endpoint.id
            }
            {
              id: customDomain.id
            }
          ]
          patternsToMatch: [
            '/*'
          ]
        }
      ]
    }
  }
}

output frontDoorEndpoint string = endpoint.properties.hostName
