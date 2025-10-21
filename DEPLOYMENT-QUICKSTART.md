# PoFunQuiz - Azure Deployment Quick Reference

## 🚀 Deploy to Azure (One Command)

```powershell
azd up
```

This deploys everything: infrastructure + code!

---

## 📋 Prerequisites Checklist

- [ ] Azure Developer CLI (`azd`) installed
- [ ] Azure CLI (`az`) installed
- [ ] Logged in: `azd auth login`
- [ ] .NET 9.0 SDK installed
- [ ] Repository cloned locally

---

## 🏗️ What Gets Created

**Resource Group**: `PoFunQuiz` (eastus2)

| Resource | Name | Purpose |
|----------|------|---------|
| Log Analytics Workspace | `PoFunQuiz` | Stores telemetry data |
| Application Insights | `PoFunQuiz` | App monitoring |
| Storage Account | `pofunquiz` | Table/Blob storage |
| App Service | `PoFunQuiz` | Hosts Blazor app |

**Uses from PoShared**:
- App Service Plan: `PoSharedAppServicePlan`
- Azure OpenAI: `posharedopenaieastus`

---

## 💻 Local Development

### Start Azurite
```powershell
azurite --silent --location ./azurite
```

### Run Application
```powershell
dotnet run --project PoFunQuiz.Server
```

### Run Tests
```powershell
dotnet test
```

---

## 🔧 Common Commands

```powershell
# Deploy infrastructure + code
azd up

# Deploy code only (faster)
azd deploy

# Provision infrastructure only
azd provision

# Show deployment status
azd show

# View environment variables
azd env get-values

# Delete all resources
azd down

# View logs
az webapp log tail --name PoFunQuiz --resource-group PoFunQuiz
```

---

## ✅ Verification

After deployment, verify:

1. **App is running**: Visit the URL from `azd show`
2. **Health check**: `https://pofunquiz.azurewebsites.net/api/health`
3. **Application Insights**: Check Azure Portal → PoFunQuiz → Application Insights

---

## 🛠️ Troubleshooting

### Issue: "PoSharedAppServicePlan not found"

**Solution**: Create the resource or update `infra/resources.bicep` to reference a different plan.

### Issue: Storage account name taken

**Solution**: Change the name in `infra/resources.bicep`:
```bicep
name: 'pofunquiz2024' // Make globally unique
```

### Issue: Deployment fails

```powershell
# View detailed logs
azd up --debug

# Check Azure Portal for error messages
# Go to: Resource Groups → Deployments
```

---

## 💰 Estimated Cost

- **Monthly**: < $5 (excluding shared resources)
- **Storage**: ~$0.02/GB
- **Monitoring**: Free tier (first 5GB)

---

## 📚 Full Documentation

- **Detailed Guide**: `docs/AZURE-DEPLOYMENT.md`
- **Testing Guide**: `docs/TESTING.md`
- **Phase 3 Summary**: `docs/PHASE3-SUMMARY.md`

---

## 🎯 Success Metrics

- ✅ Deployment completes without errors
- ✅ Health check returns "Healthy"
- ✅ Can create and play quiz games
- ✅ Application Insights shows telemetry
- ✅ No Azure cost alerts triggered

---

**Need Help?** Check the full documentation in `docs/AZURE-DEPLOYMENT.md`
