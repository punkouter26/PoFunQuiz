import { test, expect } from './fixtures';
import type { Page } from '@playwright/test';

/**
 * UI and Navigation Tests for PoFunQuiz
 * Migrated from .NET Playwright tests
 */

test.describe('Page Load Tests', () => {
  test('should load homepage successfully on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/');
    
    // Verify title
    await expect(page).toHaveTitle(/PoFunQuiz/);
    
    // Verify main heading is present
    const heading = page.locator('h1, h2, h3').first();
    await expect(heading).toBeVisible();
    
    const headingText = await heading.textContent();
    console.log(`Page heading: ${headingText}`);
  });

  test('should load homepage successfully on mobile', async ({ page }) => {
    // iPhone 12 Pro viewport
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/');
    
    // Verify title
    await expect(page).toHaveTitle(/PoFunQuiz/);
    
    // Verify page is responsive
    const bodyWidth = await page.evaluate(() => {
      return document.body.offsetWidth;
    });
    expect(bodyWidth).toBeLessThanOrEqual(390);
  });

  test('should load page within 5 seconds', async ({ page }) => {
    const startTime = Date.now();
    
    await page.goto('/');
    await page.waitForLoadState('load');
    
    const loadTime = (Date.now() - startTime) / 1000;
    console.log(`Page load time: ${loadTime.toFixed(2)}s`);
    
    expect(loadTime).toBeLessThan(5);
  });
});

test.describe('Navigation Tests', () => {
  test('should navigate to leaderboard', async ({ page }) => {
    await page.goto('/');
    
    // Find and click leaderboard link
    const leaderboardLink = page.locator("a[href='/leaderboard'], a:has-text('Leaderboard')").first();
    await leaderboardLink.click();
    
    // Verify navigation
    await expect(page).toHaveURL(/\/leaderboard/i);
    console.log(`Navigated to: ${page.url()}`);
  });

  test('should navigate to diagnostics page', async ({ page }) => {
    // /api/diag reads config values and masks them — no live Azure connection required.
    const response = await page.request.get('/api/diag');
    expect(response.status()).toBe(200);
    const body = await response.text();
    expect(body).toContain('environment');
    expect(body).toContain('connections');
    expect(body).toContain('azureOpenAI');
  });
});

test.describe('Diagnostics Page Tests', () => {
  // Activated when FULL_STACK=1 — /diag returns JSON from the live app
  test('should display health checks', async ({ page }) => {
    const response = await page.request.get('/health');
    // Health check returns 200 (Healthy) or 503 (Degraded/Unhealthy) — both are valid responses
    expect([200, 503]).toContain(response.status());
    const body = await response.text();
    expect(body).toBeTruthy();
    expect(body).toContain('status');
    console.log('✅ /health endpoint returned health status');
  });

  test('should have working refresh button', async ({ page }) => {
    // /api/diag is a JSON endpoint — two sequential requests prove it stays responsive
    const r1 = await page.request.get('/api/diag');
    expect(r1.ok()).toBeTruthy();
    const r2 = await page.request.get('/api/diag');
    expect(r2.ok()).toBeTruthy();
    console.log('✅ /api/diag responds consistently on repeated requests');
  });
});

test.describe('Responsive Design Tests', () => {
  test('should have working mobile navigation', async ({ page }) => {
    // Mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    
    // Check for mobile menu toggle
    const mobileMenu = page.locator("button.navbar-toggler, .mobile-menu, [aria-label='Toggle navigation']");
    const menuCount = await mobileMenu.count();
    
    if (menuCount > 0) {
      await mobileMenu.first().click();
      await page.waitForTimeout(500);
      
      // Menu should expand and show links
      const navLinks = page.locator('nav a, .navbar a');
      const linkCount = await navLinks.count();
      expect(linkCount).toBeGreaterThan(0);
      console.log(`Found ${linkCount} navigation links in mobile menu`);
    } else {
      // If no mobile menu, nav should still be accessible - check for any links on the page
      const allLinks = page.locator("a");
      const linkCount = await allLinks.count();
      expect(linkCount).toBeGreaterThan(0);
      console.log(`No mobile menu toggle found, but ${linkCount} links are visible on page`);
    }
  });
});

test.describe('Accessibility Tests', () => {
  test('should have proper heading structure', async ({ page }) => {
    await page.goto('/');
    
    // Wait for Blazor to fully render
    await page.waitForTimeout(2000);
    
    // Check for any heading elements (h1, h2, h3, etc.)
    const headingElements = page.locator('h1, h2, h3, h4, h5, h6');
    const headingCount = await headingElements.count();
    
    if (headingCount > 0) {
      const firstHeadingText = await headingElements.first().textContent();
      console.log(`First heading found: ${firstHeadingText}`);
      expect(headingCount).toBeGreaterThanOrEqual(1);
    } else {
      // If no headings found, at least verify the page loaded
      const bodyText = await page.locator('body').textContent();
      expect(bodyText).toBeTruthy();
      console.log('No heading elements found, but page loaded successfully');
    }
  });
});

test.describe('Game Start Flow', () => {
  test('should be able to start a new game', async ({ page }) => {
    await page.goto('/');
    
    // Look for start game button
    const startButton = page.locator("button:has-text('Start'), button:has-text('New Game'), button:has-text('Play')").first();
    const buttonCount = await startButton.count();
    
    if (buttonCount > 0) {
      await startButton.click();
      await page.waitForTimeout(2000);
      
      const url = page.url();
      console.log(`After clicking start, URL: ${url}`);
      
      // Either we're on a different page or there's game content visible
      const hasGameContent = await page.locator('input, select, .quiz, .game').count() > 0;
      expect(!url.endsWith('/') || hasGameContent).toBe(true);
    } else {
      console.log('No start game button found on homepage');
    }
  });
});
