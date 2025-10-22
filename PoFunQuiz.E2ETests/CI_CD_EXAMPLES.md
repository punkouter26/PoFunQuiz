# Example CI/CD Integration for E2E Tests

## Option 1: Separate Jobs (Recommended)

```yaml
name: Build, Test & Deploy

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  # Fast local E2E tests on every commit
  e2e-local:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install E2E dependencies
        working-directory: PoFunQuiz.E2ETests
        run: |
          npm ci
          npx playwright install chromium --with-deps
      
      - name: Run E2E Tests (Local)
        working-directory: PoFunQuiz.E2ETests
        run: npm test
      
      - name: Upload test results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report-local
          path: PoFunQuiz.E2ETests/playwright-report/

  # Deploy to Azure (only on main branch)
  deploy:
    needs: e2e-local
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      # ... your existing deployment steps ...

  # Validate Azure deployment (only after successful deploy)
  e2e-azure:
    needs: deploy
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install E2E dependencies
        working-directory: PoFunQuiz.E2ETests
        run: |
          npm ci
          npx playwright install chromium --with-deps
      
      - name: Wait for Azure deployment (cold start)
        run: sleep 60
      
      - name: Run E2E Tests (Azure)
        working-directory: PoFunQuiz.E2ETests
        run: npm run test:azure
        env:
          AZURE_APP_URL: https://pofunquiz.azurewebsites.net
      
      - name: Upload test results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report-azure
          path: PoFunQuiz.E2ETests/playwright-report/
```

## Option 2: Combined Job (Simpler)

```yaml
name: Build, Test & Deploy

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-deploy-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      # ... your existing build and deploy steps ...
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install E2E dependencies
        working-directory: PoFunQuiz.E2ETests
        run: |
          npm ci
          npx playwright install chromium --with-deps
      
      # Always run local tests
      - name: Run E2E Tests (Local)
        working-directory: PoFunQuiz.E2ETests
        run: npm test
      
      # Run Azure tests only on main branch after deployment
      - name: Run E2E Tests (Azure)
        if: github.ref == 'refs/heads/main'
        working-directory: PoFunQuiz.E2ETests
        run: |
          sleep 60  # Wait for cold start
          npm run test:azure
        env:
          AZURE_APP_URL: https://pofunquiz.azurewebsites.net
      
      - name: Upload test results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: PoFunQuiz.E2ETests/playwright-report/
```

## Option 3: Minimal (Add to existing workflow)

Add this after your deployment step in `.github/workflows/main.yml`:

```yaml
      # Add after the Azure deployment step
      - name: Setup Node for E2E Tests
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Run E2E Tests against Azure
        working-directory: PoFunQuiz.E2ETests
        run: |
          npm ci
          npx playwright install chromium --with-deps
          sleep 60
          npm run test:azure
        continue-on-error: true  # Don't fail deployment if E2E fails
```

## Cost Considerations

### Free Tier Limits
- **GitHub Actions**: 2,000 minutes/month (free)
- **Azure OpenAI**: Pay per token
- **Azure Table Storage**: Free for small usage

### Optimize Costs
```yaml
# Only run Azure tests on main branch
- name: E2E Tests (Azure)
  if: github.ref == 'refs/heads/main'
  run: npm run test:azure

# Or only on releases
- name: E2E Tests (Azure)
  if: startsWith(github.ref, 'refs/tags/v')
  run: npm run test:azure
```

## Recommended Approach

**For your current setup:**
1. Keep your existing simple CI/CD workflow
2. Add Azure E2E tests as a **separate manual job** or **scheduled job**
3. Run locally before releases

```yaml
# Scheduled Azure validation (every night)
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM UTC daily

jobs:
  e2e-azure-nightly:
    runs-on: ubuntu-latest
    steps:
      # ... run Azure E2E tests ...
```

This gives you validation without slowing down every commit or increasing costs.
