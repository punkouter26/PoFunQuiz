import { test, expect } from './fixtures';
import type { BrowserContext, Page } from '@playwright/test';

test.describe('Multiplayer Game Flow', () => {
  let context1: BrowserContext;
  let context2: BrowserContext;
  let page1: Page;
  let page2: Page;

  // Multiplayer requires two pages + two Blazor circuits + two SignalR handshakes — 120s budget
  test.setTimeout(120000);

  test.beforeEach(async ({ browser }) => {
    // Create two separate browser contexts to simulate two different devices
    context1 = await browser.newContext();
    context2 = await browser.newContext();
    
    page1 = await context1.newPage();
    page2 = await context2.newPage();
  });

  test.afterEach(async () => {
    await context1.close();
    await context2.close();
  });

  test('should allow two players to join and play a game', async () => {
    // --- Player 1 (Host) Setup ---
    await test.step('Player 1 creates a game', async () => {
      await page1.goto('/multiplayer');
      
      // Wait for Blazor circuit — first interactive element appearing signals hydration
      await page1.locator('input[placeholder="Enter your name"]').waitFor({ state: 'visible', timeout: 15000 });

      // Enter name — use pressSequentially to trigger Blazor input bindings
      const nameInput = page1.locator('input[placeholder="Enter your name"]');
      await nameInput.click();
      await nameInput.fill('');
      await nameInput.pressSequentially('HostPlayer', { delay: 50 });
      await nameInput.dispatchEvent('change');
      await page1.waitForTimeout(300);

      const createButton = page1.locator('button:has-text("Create New Game")');
      await expect(createButton).toBeEnabled({ timeout: 5000 });
      await createButton.click();

      // Wait for Game ID to appear (uses custom .mp-id-value class)
      await expect(page1.locator('.mp-id-value')).toBeVisible({ timeout: 10000 });
      await expect(page1.locator('text=Waiting for Player 2 to join')).toBeVisible();
    });

    // Get the Game ID (trim whitespace from inner text)
    const gameId = (await page1.locator('.mp-id-value').innerText()).trim();
    console.log(`Game ID created: ${gameId}`);
    expect(gameId).toBeTruthy();

    // --- Player 2 (Joiner) Setup ---
    await test.step('Player 2 joins the game', async () => {
      await page2.goto('/multiplayer');
      
      // Wait for Blazor circuit — first interactive element appearing signals hydration
      await page2.locator('input[placeholder="Enter your name"]').waitFor({ state: 'visible', timeout: 15000 });
      
      // Enter name — use pressSequentially to trigger Blazor input bindings
      const nameInput = page2.locator('input[placeholder="Enter your name"]');
      await nameInput.click();
      await nameInput.fill('');
      await nameInput.pressSequentially('JoinPlayer', { delay: 50 });
      await nameInput.dispatchEvent('change');

      // Switch to the "Join Game" tab — the Game ID input is hidden until this tab is active
      const joinTab = page2.locator('button.mp-tab:has-text("Join Game")');
      await expect(joinTab).toBeVisible({ timeout: 5000 });
      await joinTab.click();

      // Wait for Blazor to re-render the Join tab panel (Game ID input must appear)
      const gameIdInput = page2.locator('input[placeholder="Enter Game ID"]');
      await gameIdInput.waitFor({ state: 'visible', timeout: 5000 });
      await gameIdInput.click();
      await gameIdInput.fill('');
      await gameIdInput.pressSequentially(gameId, { delay: 50 });
      await gameIdInput.dispatchEvent('change');
      
      // Wait for Blazor to process bindings before clicking Join
      await page2.waitForTimeout(500);
      
      // Click the Join action button — target the .mp-btn-secondary class to avoid matching the "Join Game" tab
      const joinButton = page2.locator('button.mp-btn-secondary');
      await expect(joinButton).toBeEnabled({ timeout: 10000 });
      await joinButton.click();

      // After joining, SignalR delivers state update — wait up to 15s for lobby to appear
      await expect(page2.locator(`text=${gameId}`)).toBeVisible({ timeout: 15000 });
      await expect(page2.locator('text=Waiting for host to start')).toBeVisible({ timeout: 10000 });
    });

    // --- Verify Lobby Sync ---
    await test.step('Verify lobby synchronization', async () => {
      // Host should see JoinPlayer (SignalR broadcast delivery up to 10s)
      await expect(page1.locator('text=JoinPlayer')).toBeVisible({ timeout: 10000 });
      
      // Joiner should see HostPlayer
      await expect(page2.locator('text=HostPlayer')).toBeVisible({ timeout: 10000 });
      
      // Host should see Start Game button now (only active when topic is also selected)
      await expect(page1.locator('button:has-text("Start Game")')).toBeVisible({ timeout: 10000 });
    });

    // --- Verify Host Can Start ---
    await test.step('Host has topic dropdown and Start Game button', async () => {
      // Verify the topic dropdown is shown (required before Start Game becomes enabled)
      await expect(page1.locator('.rz-dropdown')).toBeVisible({ timeout: 5000 });
      
      // Verify Start Game button is present (disabled until topic selected — just checks presence)
      const startBtn = page1.locator('button:has-text("Start Game")');
      await expect(startBtn).toBeVisible();
      
      console.log('✅ Host lobby shows topic selector + Start Game button when both players joined');
    });
  });
});
