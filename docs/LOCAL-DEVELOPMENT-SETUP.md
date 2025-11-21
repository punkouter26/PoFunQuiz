# Local Development Setup

## Prerequisites

1. **.NET 10 SDK** installed
2. **Azure CLI** installed and authenticated
3. **Azurite** running for local storage emulation

## Environment Configuration

### 1. Set Azure Key Vault Endpoint

Set the environment variable to point to your Azure Key Vault:

**PowerShell:**
```powershell
$env:AZURE_KEY_VAULT_ENDPOINT = "https://pofunquiz-kv.vault.azure.net/"
```

**Or add to your user environment variables permanently:**
```powershell
[System.Environment]::SetEnvironmentVariable('AZURE_KEY_VAULT_ENDPOINT', 'https://pofunquiz-kv.vault.azure.net/', 'User')
```

### 2. Authenticate with Azure

```powershell
az login
```

Ensure your Azure account has the **Key Vault Secrets User** role on the `pofunquiz-kv` Key Vault.

### 3. Start Azurite

Start Azurite for local storage emulation:

```powershell
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

Or if Azurite is already configured to start automatically, ensure it's running.

## How It Works

### Configuration Flow

1. **Key Vault Connection**: App reads `AZURE_KEY_VAULT_ENDPOINT` from environment variable
2. **Authentication**: Uses `DefaultAzureCredential` (Azure CLI credentials locally)
3. **Secret Retrieval**: Loads secrets from Key Vault:
   - `AzureOpenAI--ApiKey` → OpenAI API key
   - `TableStorageConnectionString` → Azure Storage (overridden for local)
   - `ApplicationInsights--ConnectionString` → Application Insights
4. **Storage Override**: In Development environment, storage connection is overridden to `UseDevelopmentStorage=true` (Azurite)

### What Gets Loaded

| Secret | Source (Local) | Source (Production) |
|--------|----------------|---------------------|
| OpenAI API Key | Key Vault | Key Vault |
| Table Storage | **Azurite** (override) | Key Vault |
| Application Insights | Key Vault | Key Vault |

## Running the Application

### Option 1: Visual Studio Code (F5)
1. Open workspace in VS Code
2. Press **F5** to debug
3. App will start and connect to Key Vault + Azurite

### Option 2: Command Line
```powershell
cd src/PoFunQuiz.Api
dotnet run
```

### Option 3: Watch Mode
```powershell
cd src/PoFunQuiz.Api
dotnet watch run
```

## Verifying Configuration

Check the console output on startup for these log messages:

✅ **Success:**
```
[INF] Configured Azure Key Vault: https://pofunquiz-kv.vault.azure.net/
[INF] Loaded OpenAI API key from Key Vault
[INF] Loaded Table Storage connection string from Key Vault
[INF] Development environment detected: Using Azurite for Table Storage
```

❌ **Issues:**
```
[WRN] AZURE_KEY_VAULT_ENDPOINT not configured; skipping Key Vault integration
```
→ Set the environment variable (see step 1 above)

## Troubleshooting

### "Authentication failed" when accessing Key Vault

**Solution:**
1. Run `az login` to authenticate
2. Verify you have access to the Key Vault:
   ```powershell
   az keyvault secret list --vault-name pofunquiz-kv
   ```
3. Request access from the Key Vault administrator if needed

### "Table Storage connection failed"

**Solution:**
1. Ensure Azurite is running
2. Check Azurite logs at `c:\azurite\debug.log`
3. Verify the connection string is being overridden (check startup logs)

### Secrets are empty

**Solution:**
1. Verify secrets exist in Key Vault:
   ```powershell
   az keyvault secret show --vault-name pofunquiz-kv --name AzureOpenAI--ApiKey
   ```
2. Check RBAC permissions on Key Vault
3. Restart the application after setting environment variables

## Security Notes

🔒 **Important:**
- Never commit production secrets to `appsettings.json`
- All secrets are now stored in Azure Key Vault
- Local development uses the same Key Vault as production
- Storage is the only service that differs (Azurite vs Azure)

## Related Documentation

- [Key Vault Setup](../infra/KEY-VAULT-SETUP.md)
- [Copilot Instructions](../.github/copilot-instructions.md)
