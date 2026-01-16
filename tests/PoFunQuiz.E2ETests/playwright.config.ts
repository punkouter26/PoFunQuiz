import { defineConfig, devices } from '@playwright/test';

// Determine if testing against Azure or localhost
const isAzure = process.env.TEST_ENV === 'azure';
const baseURL = isAzure 
  ? (process.env.AZURE_APP_URL || 'https://pofunquiz.azurewebsites.net')
  : (process.env.BASE_URL || 'http://localhost:5000');

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1, // 1 retry to capture traces
  workers: 1,
  reporter: [['html'], ['list']],
  
  // Increase timeout for Azure (cold start can take 30-60 seconds)
  timeout: isAzure ? 90000 : 30000,
  
  use: {
    baseURL: baseURL,
    trace: 'on-first-retry', // Capture traces on failure (per PoTestAll requirements)
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    // Longer navigation timeout for Azure
    navigationTimeout: isAzure ? 60000 : 30000,
  },

  // Projects: Chromium and Mobile only (per PoTestAll requirements)
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],

  // Only start local server if NOT testing Azure
  webServer: isAzure ? undefined : {
    command: 'dotnet run --project ../../src/PoFunQuiz.Web/PoFunQuiz.Web.csproj --urls http://localhost:5000',
    url: 'http://localhost:5000',
    reuseExistingServer: true, // Always reuse if server is already running
    timeout: 120000,
  },
});
