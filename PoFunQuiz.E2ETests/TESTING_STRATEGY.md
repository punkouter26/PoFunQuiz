# E2E Testing Strategy: Local vs Azure

## ✅ Why This is a GOOD Idea

### 1. **Comprehensive Coverage**
- **Local tests**: Fast feedback during development (5-10 seconds)
- **Azure tests**: Validates real production environment (real OpenAI, real storage)

### 2. **Catches Different Issues**
- **Local**: Logic bugs, UI issues, functionality
- **Azure**: Configuration problems, deployment issues, cold start behavior, network latency

### 3. **CI/CD Flexibility**
```yaml
# Fast feedback on every PR
- name: Local E2E Tests
  run: npm test

# Full validation before production
- name: Azure E2E Tests (on main branch only)
  if: github.ref == 'refs/heads/main'
  run: npm run test:azure
```

### 4. **Cost-Effective**
- Run most tests locally (free, fast)
- Run Azure tests only when needed (avoids API costs, quota usage)

## ⚠️ Considerations & Best Practices

### 1. **Test Design**
```typescript
// ✅ GOOD: Uses baseURL from config
await page.goto('/');

// ❌ BAD: Hardcoded URL
await page.goto('http://localhost:5000/');
```

### 2. **Environment-Specific Timeouts**
```typescript
// Account for Azure F1 cold start (30-60 seconds)
test.setTimeout(isAzure ? 90000 : 30000);
```

### 3. **Skip Inappropriate Tests**
```typescript
test.skip(isAzure, 'Azurite-specific test - skip in Azure');

test('should upload to Azure Storage', async ({ page }) => {
  // This test uses Azurite locally, real storage in Azure
});
```

### 4. **Data Isolation**
- Local: Use Azurite emulator (clean state)
- Azure: Use test-specific data or cleanup after tests

## 📊 Usage Recommendations

### During Development
```bash
npm test                    # Fast local tests
npm run test:headed         # Debug with visible browser
```

### Before Committing
```bash
npm run test:local          # Verify all local tests pass
```

### Before Releasing
```bash
npm run test:both           # Test both environments
```

### In CI/CD
```bash
# On pull requests: Local only
npm test

# On main branch: Both environments
npm run test:both
```

## 🎯 Current Configuration

### Local Testing (Default)
- **URL**: http://localhost:5000
- **Server**: Auto-starts with `dotnet run`
- **Timeout**: 30 seconds
- **Dependencies**: Azurite, local OpenAI config

### Azure Testing
- **URL**: https://pofunquiz.azurewebsites.net
- **Server**: Already running (F1 tier)
- **Timeout**: 90 seconds (handles cold start)
- **Dependencies**: Azure OpenAI, Azure Table Storage

## 🚀 Quick Start

```bash
# Run against localhost (default)
npm test

# Run against Azure
npm run test:azure

# Run a specific test against Azure
npm run test:azure -- --grep "homepage"

# Custom Azure URL
TEST_ENV=azure AZURE_APP_URL=https://staging.azurewebsites.net npm test
```

## 🔍 Debugging Azure Tests

### View the failing page
The test will capture screenshots and videos in `test-results/`

### Check Azure is awake
```bash
# Wake up the F1 tier (takes 30-60 seconds)
curl https://pofunquiz.azurewebsites.net
```

### Run with visible browser
```bash
npm run test:azure:headed
```

## ✅ Bottom Line

**This is a GOOD strategy** that gives you:
- Fast development cycles (local)
- Production validation (Azure)
- Flexible CI/CD options
- Cost-effective testing

The key is to use local tests frequently and Azure tests strategically (before releases, after config changes, in CI/CD on main branch).
