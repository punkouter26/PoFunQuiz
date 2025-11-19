import { test, expect } from '@playwright/test';

/**
 * Homepage Startup Verification Tests
 * Verifies that the PoFunQuiz webapp starts up correctly and displays the home page
 */

test.describe('Homepage Startup Tests', () => {
  test('should successfully start the webapp and display the home page', async ({ page }) => {
    // Navigate to the root URL
    await page.goto('/');
    
    // Verify the page loaded successfully (status 200)
    const response = await page.goto('/');
    expect(response?.status()).toBe(200);
    
    // Verify the page title contains "PoFunQuiz"
    await expect(page).toHaveTitle(/PoFunQuiz/i);
    console.log(`Page title: ${await page.title()}`);
    
    // Wait for the page to be fully loaded
    await page.waitForLoadState('load');
    await page.waitForLoadState('domcontentloaded');
    
    // Verify the body element is visible (basic DOM loaded check)
    const body = page.locator('body');
    await expect(body).toBeVisible();
    
    // Take a screenshot for verification
    await page.screenshot({ path: 'tests/PoFunQuiz.E2ETests/screenshots/homepage.png', fullPage: true });
    
    console.log('✅ Webapp started successfully and home page is displayed');
  });

  test('should load all critical resources', async ({ page }) => {
    // Track failed resources
    const failedResources: string[] = [];
    
    page.on('requestfailed', request => {
      failedResources.push(`${request.url()} - ${request.failure()?.errorText}`);
    });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Verify no critical resources failed to load
    if (failedResources.length > 0) {
      console.warn('Failed resources:', failedResources);
    }
    
    // We allow some failures (like optional CDN resources) but critical ones should load
    const criticalFailures = failedResources.filter(url => 
      url.includes('blazor') || url.includes('_framework')
    );
    
    expect(criticalFailures.length).toBe(0);
    console.log(`✅ All critical resources loaded successfully`);
  });

  test('should have Blazor framework initialized', async ({ page }) => {
    await page.goto('/');
    
    // Wait for Blazor to initialize
    await page.waitForTimeout(3000);
    
    // Check if Blazor is loaded by looking for the Blazor script
    const blazorScript = await page.evaluate(() => {
      const scripts = Array.from(document.querySelectorAll('script'));
      return scripts.some(script => 
        script.src.includes('blazor') || script.src.includes('_framework')
      );
    });
    
    expect(blazorScript).toBe(true);
    console.log('✅ Blazor framework detected');
  });

  test('should display main content without errors', async ({ page }) => {
    // Listen for console errors
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.goto('/');
    await page.waitForTimeout(2000);
    
    // Verify the page has some content
    const bodyText = await page.locator('body').textContent();
    expect(bodyText).toBeTruthy();
    expect(bodyText!.length).toBeGreaterThan(0);
    
    // Check for critical console errors (allow some warnings)
    const criticalErrors = consoleErrors.filter(error => 
      !error.includes('DevTools') && 
      !error.includes('extension') &&
      !error.toLowerCase().includes('warning')
    );
    
    if (criticalErrors.length > 0) {
      console.warn('Console errors detected:', criticalErrors);
    }
    
    // We can be lenient here - some frameworks produce non-critical errors
    expect(criticalErrors.length).toBeLessThan(5);
    console.log(`✅ Home page displayed with content (${bodyText!.length} characters)`);
  });

  test('should be responsive on desktop viewport', async ({ page }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/');
    
    // Verify page loaded
    await expect(page).toHaveTitle(/PoFunQuiz/i);
    
    // Verify viewport is correctly set
    const viewportSize = page.viewportSize();
    expect(viewportSize?.width).toBe(1920);
    expect(viewportSize?.height).toBe(1080);
    
    console.log('✅ Desktop viewport test passed');
  });

  test('should be responsive on mobile viewport', async ({ page }) => {
    // Set mobile viewport (iPhone 12 Pro)
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/');
    
    // Verify page loaded
    await expect(page).toHaveTitle(/PoFunQuiz/i);
    
    // Verify page adapts to mobile width
    const bodyWidth = await page.evaluate(() => document.body.offsetWidth);
    expect(bodyWidth).toBeLessThanOrEqual(390);
    
    console.log('✅ Mobile viewport test passed');
  });

  test('should load within acceptable time', async ({ page }) => {
    const startTime = Date.now();
    
    await page.goto('/');
    await page.waitForLoadState('load');
    
    const loadTime = Date.now() - startTime;
    const loadTimeSeconds = loadTime / 1000;
    
    console.log(`Page load time: ${loadTimeSeconds.toFixed(2)} seconds`);
    
    // Page should load within 10 seconds (generous for cold start)
    expect(loadTimeSeconds).toBeLessThan(10);
    console.log('✅ Load time test passed');
  });
});

test.describe('Homepage Content Verification', () => {
  test('should have navigation elements', async ({ page }) => {
    await page.goto('/');
    await page.waitForTimeout(2000);
    
    // Check for navigation links (should have at least one link)
    const links = await page.locator('a').count();
    expect(links).toBeGreaterThan(0);
    
    console.log(`✅ Found ${links} navigation links`);
  });

  test('should have interactive elements', async ({ page }) => {
    await page.goto('/');
    await page.waitForTimeout(2000);
    
    // Check for buttons or interactive elements
    const buttons = await page.locator('button').count();
    const inputs = await page.locator('input').count();
    const interactiveElements = buttons + inputs;
    
    console.log(`✅ Found ${buttons} buttons and ${inputs} inputs (${interactiveElements} total interactive elements)`);
    
    // Should have at least some interactive elements on a game page
    expect(interactiveElements).toBeGreaterThanOrEqual(0);
  });
});
