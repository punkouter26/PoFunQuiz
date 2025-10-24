# PoFunQuiz - E2E Testing Guide

Complete guide for end-to-end testing using Playwright and TypeScript.

---

## Prerequisites

- Node.js 18+ installed
- .NET 9.0 SDK installed
- PoFunQuiz application built

---

## Setup

```bash
# Install dependencies
npm install

# Install Playwright browsers
npx playwright install chromium
```

---

## Running Tests

### Local Development (Default)
```bash
# Run all tests against localhost
npm test

# Run tests with browser visible
npm run test:headed

# Run tests in debug mode
npm run test:debug

# Run tests in UI mode (interactive)
npm run test:ui

# View test report
npm run test:report
```

### Azure Production
```bash
# Run all tests against Azure deployment
npm run test:azure

# Run with visible browser against Azure
npm run test:azure:headed
```

---

## Testing Strategy: Local vs Azure

### Why Both Environments?

#### Local Tests (Fast Feedback)
- **Speed:** 5-10 seconds per test
- **Cost:** Free
- **Purpose:** Logic bugs, UI issues, functionality
- **When:** Every commit, every PR

#### Azure Tests (Production Validation)
- **Speed:** 30-90 seconds (F1 cold start)
- **Cost:** API calls, quota usage
- **Purpose:** Configuration, deployment, real services
- **When:** Main branch, pre-release

### Comprehensive Coverage
```yaml
# CI/CD Strategy
- Local E2E: Every PR (fast feedback)
- Azure E2E: Main branch only (production validation)
```

---

## Test Configuration

### Environment-Specific Settings

```typescript
// Detect environment
const isAzure = process.env.BASE_URL?.includes('azurestaticapps.net');

// Adjust timeouts
test.setTimeout(isAzure ? 90000 : 30000);

// Skip inappropriate tests
test.skip(isAzure, 'Azurite-specific - skip in Azure');
```

### Best Practices

✅ **GOOD:**
```typescript
// Use baseURL from config
await page.goto('/');
await expect(page).toHaveURL(/gamesetup/);
```

❌ **BAD:**
```typescript
// Hardcoded URLs
await page.goto('http://localhost:5000/');
```

---

## CI/CD Integration

### GitHub Actions - Separate Jobs (Recommended)

```yaml
name: Build, Test & Deploy

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  # Fast local E2E on every commit
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
      
      - name: Install dependencies
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

  # Deploy to Azure (main branch only)
  deploy:
    needs: e2e-local
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure
        run: azd deploy
  
  # Azure E2E validation (after deployment)
  e2e-azure:
    needs: deploy
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      
      - name: Install dependencies
        working-directory: PoFunQuiz.E2ETests
        run: |
          npm ci
          npx playwright install chromium --with-deps
      
      - name: Run E2E Tests (Azure)
        working-directory: PoFunQuiz.E2ETests
        run: npm run test:azure
        env:
          BASE_URL: ${{ secrets.AZURE_APP_URL }}
      
      - name: Upload test results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report-azure
          path: PoFunQuiz.E2ETests/playwright-report/
```

---

## Test Structure

### Example Test File

```typescript
import { test, expect } from '@playwright/test';

test.describe('Game Flow', () => {
  test('should complete full quiz game', async ({ page }) => {
    // Navigate to app
    await page.goto('/');
    
    // Start new game
    await page.getByRole('button', { name: /start/i }).click();
    
    // Verify game setup loaded
    await expect(page).toHaveURL(/gamesetup/);
    
    // Wait for questions to load
    await expect(page.getByText(/generating/i)).toBeHidden({ timeout: 30000 });
    
    // Game should start
    await expect(page).toHaveURL(/gameboard/);
  });
});
```

---

## Troubleshooting

### Common Issues

#### 1. Azure F1 Cold Start Timeout
```typescript
// Increase timeout for Azure
test.setTimeout(90000);
```

#### 2. Local Server Not Started
```bash
# Start local server first
cd PoFunQuiz.Server
dotnet watch run
```

#### 3. Playwright Browsers Not Installed
```bash
npx playwright install chromium --with-deps
```

#### 4. Port Already in Use
```bash
# Change port in playwright.config.ts
webServer: {
  command: 'dotnet run --project ../PoFunQuiz.Server',
  url: 'http://localhost:5001',
  reuseExistingServer: true
}
```

---

## Test Reports

### View HTML Report
```bash
npm run test:report
```

### View Trace on Failure
```bash
npx playwright show-trace playwright-report/trace.zip
```

---

## Best Practices

### 1. Use Semantic Locators
```typescript
// ✅ GOOD: Accessible, resilient
await page.getByRole('button', { name: /start/i });
await page.getByLabel('Player Name');

// ❌ BAD: Brittle, breaks easily
await page.click('#btn-123');
await page.locator('.css-class-xyz');
```

### 2. Wait for State, Not Time
```typescript
// ✅ GOOD: Wait for element
await expect(page.getByText('Loading')).toBeHidden();

// ❌ BAD: Arbitrary timeout
await page.waitForTimeout(5000);
```

### 3. Avoid Test Interdependence
```typescript
// ✅ GOOD: Each test independent
test('test 1', async ({ page }) => {
  await page.goto('/');
  // ... test logic
});

test('test 2', async ({ page }) => {
  await page.goto('/');
  // ... test logic
});

// ❌ BAD: Tests depend on order
test.describe.serial('dependent tests', () => {
  // Don't do this!
});
```

### 4. Clean Up Test Data
```typescript
test.afterEach(async ({ page }) => {
  // Clear storage after each test
  await page.evaluate(() => localStorage.clear());
});
```

---

## Performance Tips

### Parallel Execution
```bash
# Run tests in parallel (default: CPU cores / 2)
npm test

# Specify worker count
npx playwright test --workers=4
```

### Headed vs Headless
```bash
# Headless (faster, CI-friendly)
npm test

# Headed (debugging, local dev)
npm run test:headed
```

### Selective Testing
```bash
# Run specific test file
npx playwright test tests/game-flow.spec.ts

# Run tests matching pattern
npx playwright test --grep "game setup"
```

---

## Cost Optimization

### Minimize Azure API Calls
- Run most tests locally
- Azure tests only on main branch
- Use test doubles for expensive operations
- Consider quota limits (OpenAI, Azure)

### Example Strategy
```yaml
# PR builds: Local E2E only (fast, free)
pull_request:
  jobs:
    - e2e-local

# Main branch: Full validation (local + Azure)
push:
  branches: [ main ]
  jobs:
    - e2e-local
    - deploy
    - e2e-azure
```

---

## Resources

- **Playwright Documentation:** https://playwright.dev
- **Best Practices:** https://playwright.dev/docs/best-practices
- **CI/CD Examples:** https://playwright.dev/docs/ci

---

**Last Updated:** October 23, 2025
