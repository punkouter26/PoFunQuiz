# Azure Key Vault Setup

## Overview

PoFunQuiz uses Azure Key Vault to securely store and manage sensitive configuration values like API keys and connection strings. This follows security best practices by:

- Removing secrets from source code and configuration files
- Using Azure RBAC for granular access control
- Enabling audit logging of secret access
- Supporting secret rotation without code changes

## Architecture

### Key Vault Resource
- **Name**: `PoFunQuiz-kv`
- **Location**: Same as resource group
- **Authorization**: RBAC-based (Azure role assignments)
- **Soft Delete**: Enabled (7-day retention)

### Stored Secrets

| Secret Name | Purpose | Source |
|-------------|---------|--------|
| `TableStorageConnectionString` | Azure Table Storage authentication | Auto-generated from storage account keys |
| `AzureOpenAI--ApiKey` | Azure OpenAI API authentication | Stored during deployment |

### Access Control

The App Service (`PoFunQuiz-app`) uses its **System-Assigned Managed Identity** to access Key Vault:

1. App Service has system-assigned identity enabled
2. Identity is granted **Key Vault Secrets User** role on the Key Vault
3. No credentials are stored in code or configuration

## Local Development vs Production

### Local Development
- **Key Vault is accessed** via `AZURE_KEY_VAULT_ENDPOINT` environment variable
- Secrets (API keys, etc.) are retrieved from Key Vault using `DefaultAzureCredential`
- **Storage connection string is overridden** to use Azurite: `UseDevelopmentStorage=true`
- Requires Azure CLI authentication: `az login`

### Production (Azure App Service)
- Key Vault is automatically configured via `AZURE_KEY_VAULT_ENDPOINT` environment variable
- Managed identity authentication is used
- Production storage connection string is used from Key Vault

## Configuration in Code

### Program.cs
```csharp
// Add Azure Key Vault configuration for ALL environments when endpoint is provided
var keyVaultEndpoint = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new Azure.Identity.DefaultAzureCredential());
    Log.Information("Configured Azure Key Vault: {KeyVaultEndpoint}", keyVaultEndpoint);
    
    // Map Key Vault secrets to configuration paths
    // ... secret mapping code ...
    
    // Override storage connection string for local development (use Azurite)
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration["AppSettings:Storage:TableStorageConnectionString"] = "UseDevelopmentStorage=true";
        Log.Information("Development environment detected: Using Azurite for Table Storage");
    }
}
```

### Bicep Configuration

The Key Vault and secrets are provisioned in `infra/resources.bicep`:

```bicep
// Key Vault resource
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'PoFunQuiz-kv'
  properties: {
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Secret stored in Key Vault
resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'TableStorageConnectionString'
  properties: {
    value: 'DefaultEndpointsProtocol=https;...'
  }
}

// App Service references secret
appSettings: [
  {
    name: 'AppSettings__Storage__TableStorageConnectionString'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=TableStorageConnectionString)'
  }
]

// Grant access via RBAC
resource keyVaultSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  properties: {
    roleDefinitionId: '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
    principalId: appService.identity.principalId
  }
}
```

## Deployment

Deploy infrastructure using Azure Developer CLI:

```powershell
# Login to Azure
azd auth login

# Provision and deploy
azd up
```

The deployment will:
1. Create the Key Vault (`PoFunQuiz-kv`)
2. Store secrets in Key Vault
3. Configure App Service with managed identity
4. Grant RBAC permissions
5. Set environment variables to reference Key Vault

## Managing Secrets

### Adding a New Secret

1. **Update Bicep**: Add secret resource in `infra/resources.bicep`
```bicep
resource newSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'MyNewSecret'
  properties: {
    value: 'secret-value-here'
  }
}
```

2. **Reference in App Service**: Add app setting
```bicep
{
  name: 'AppSettings__MyNewSecret'
  value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=MyNewSecret)'
}
```

3. **Deploy**: Run `azd up` to apply changes

### Rotating Secrets

1. **Update in Azure Portal**: Navigate to Key Vault → Secrets → Select secret → New Version
2. **Or via CLI**:
```powershell
az keyvault secret set --vault-name PoFunQuiz-kv --name MySecret --value "new-value"
```

3. **Restart App Service**: Changes take effect on next app restart

## Security Best Practices

✅ **Implemented**:
- RBAC authorization (no access policies)
- Managed identity (no credentials in code)
- Soft delete enabled
- Secrets stored in Key Vault (not in appsettings.json)

🔒 **Additional Recommendations**:
- Enable Key Vault firewall to restrict access
- Set up diagnostic logging
- Implement secret expiration dates
- Use separate Key Vaults for dev/staging/prod

## Troubleshooting

### App Service Can't Access Key Vault

**Symptom**: Configuration values are empty or default

**Solutions**:
1. Verify managed identity is enabled on App Service
2. Check RBAC role assignment exists
3. Confirm `AZURE_KEY_VAULT_ENDPOINT` is set correctly
4. Review App Service logs for authentication errors

### Local Development Issues

**Symptom**: Key Vault errors when running locally

**Solutions**:
1. Ensure `AZURE_KEY_VAULT_ENDPOINT` environment variable is set
2. Authenticate with Azure: `az login`
3. Verify your Azure account has **Key Vault Secrets User** role on the Key Vault
4. Check that Azurite is running for local storage: `azurite --silent --location c:\azurite --debug c:\azurite\debug.log`

### Secret Not Found

**Symptom**: `@Microsoft.KeyVault(...)` reference returns empty

**Solutions**:
1. Verify secret name matches exactly (case-sensitive)
2. Check secret exists in Key Vault
3. Confirm App Service has Secrets User role
4. Restart App Service to refresh configuration

## Related Documentation

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [App Service Key Vault References](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
- [Managed Identities](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/)
