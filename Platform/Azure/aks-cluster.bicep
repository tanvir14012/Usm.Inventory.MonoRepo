targetScope = 'resourceGroup'

@description('AKS cluster name')
param aksName string = 'aks-usm-prod'

@description('Azure region')
param location string = resourceGroup().location

@description('Kubernetes version')
param kubernetesVersion string = '1.30.3'

@description('DNS prefix for AKS API endpoint')
param dnsPrefix string = 'usm-inventory'

@description('Node VM size')
param nodeVmSize string = 'Standard_D4ds_v5'

@description('Initial node count')
@minValue(3)
param nodeCount int = 3

@description('VNet CIDR')
param vnetCidr string = '10.40.0.0/16'

@description('AKS subnet CIDR')
param aksSubnetCidr string = '10.40.1.0/24'

@description('Pod CIDR for Azure CNI overlay')
param podCidr string = '10.244.0.0/16'

@description('Service CIDR')
param serviceCidr string = '10.41.0.0/16'

@description('DNS Service IP')
param dnsServiceIP string = '10.41.0.10'

@description('Azure AD group object IDs for AKS admin access')
param adminGroupObjectIds array

@description('ACR resource ID for kubelet pull integration')
param acrResourceId string

@description('User-assigned managed identity name')
param workloadIdentityName string = 'mi-usm-workload'

resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: 'vnet-${aksName}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetCidr
      ]
    }
    subnets: [
      {
        name: 'snet-aks'
        properties: {
          addressPrefix: aksSubnetCidr
        }
      }
    ]
  }
}

resource workloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: workloadIdentityName
  location: location
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  scope: subscription()
  name: last(split(acrResourceId, '/'))
}

resource aks 'Microsoft.ContainerService/managedClusters@2024-05-01' = {
  name: aksName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    tier: 'Standard'
    name: 'Base'
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    dnsPrefix: dnsPrefix
    enableRBAC: true
    supportPlan: 'KubernetesOfficial'
    aadProfile: {
      managed: true
      enableAzureRBAC: true
      adminGroupObjectIDs: adminGroupObjectIds
    }
    oidcIssuerProfile: {
      enabled: true
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
      defender: {
        securityMonitoring: {
          enabled: true
        }
      }
    }
    networkProfile: {
      networkPlugin: 'azure'
      networkPluginMode: 'overlay'
      networkPolicy: 'azure'
      podCidr: podCidr
      serviceCidr: serviceCidr
      dnsServiceIP: dnsServiceIP
      loadBalancerSku: 'standard'
      outboundType: 'loadBalancer'
    }
    apiServerAccessProfile: {
      enablePrivateCluster: true
      enablePrivateClusterPublicFQDN: false
    }
    addonProfiles: {
      azurepolicy: {
        enabled: true
      }
    }
    agentPoolProfiles: [
      {
        name: 'systempool'
        mode: 'System'
        count: nodeCount
        vmSize: nodeVmSize
        vnetSubnetID: resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, 'snet-aks')
        osDiskType: 'Managed'
        osDiskSizeGB: 128
        maxPods: 80
        type: 'VirtualMachineScaleSets'
        enableAutoScaling: true
        minCount: 3
        maxCount: 12
        orchestratorVersion: kubernetesVersion
      }
    ]
  }
}

resource acrRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aks.id, acrResourceId, 'AcrPull')
  scope: acr
  properties: {
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

output clusterName string = aks.name
output clusterPrincipalId string = aks.identity.principalId
output workloadIdentityClientId string = workloadIdentity.properties.clientId
