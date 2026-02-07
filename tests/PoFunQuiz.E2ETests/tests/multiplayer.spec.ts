import { test, expect, BrowserContext, Page } from '@playwright/test';

test.describe('Multiplayer Game Flow', () => {
  let context1: BrowserContext;
  let context2: BrowserContext;
  let page1: Page;
  let page2: Page;

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
      
      // Wait for Blazor to initialize
      await page1.waitForTimeout(2000);
      
      await expect(page1.locator('body')).toBeVisible();

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

    // Get the Game ID
    const gameId = await page1.locator('.mp-id-value').innerText();
    console.log(`Game ID created: ${gameId}`);
    expect(gameId).toBeTruthy();

    // --- Player 2 (Joiner) Setup ---
    await test.step('Player 2 joins the game', async () => {
      await page2.goto('/multiplayer');
      
      // Wait for Blazor to initialize
      await page2.waitForTimeout(2000);
      
      // Enter name — use pressSequentially to trigger Blazor input bindings
      const nameInput = page2.locator('input[placeholder="Enter your name"]');
      await nameInput.click();
      await nameInput.fill('');
      await nameInput.pressSequentially('JoinPlayer', { delay: 50 });
      await nameInput.dispatchEvent('change');

      // Enter Game ID — same approach
      const gameIdInput = page2.locator('input[placeholder="Enter Game ID"]');
      await gameIdInput.click();
      await gameIdInput.fill('');
      await gameIdInput.pressSequentially(gameId, { delay: 50 });
      await gameIdInput.dispatchEvent('change');
      
      // Wait for Blazor to process bindings
      await page2.waitForTimeout(500);
      
      // Click Join (Radzen button text is "Join", not "Join Game")
      const joinButton = page2.locator('button:has-text("Join")').first();
      await expect(joinButton).toBeEnabled({ timeout: 10000 });
      await joinButton.click();

      // Verify P2 sees the lobby
      await expect(page2.locator(`text=${gameId}`)).toBeVisible();
      await expect(page2.locator('text=Waiting for host to start')).toBeVisible();
    });

    // --- Verify Lobby Sync ---
    await test.step('Verify lobby synchronization', async () => {
      // Host should see JoinPlayer
      await expect(page1.locator('text=JoinPlayer')).toBeVisible();
      
      // Joiner should see HostPlayer
      await expect(page2.locator('text=HostPlayer')).toBeVisible();
      
      // Host should see Start Game button now
      await expect(page1.locator('button:has-text("Start Game")')).toBeVisible();
    });

    // --- Start Game ---
    await test.step('Host starts the game', async () => {
      await page1.click('button:has-text("Start Game")');

      // Both players should see "Game in Progress!"
      await expect(page1.locator('text=Game in Progress!')).toBeVisible();
      await expect(page2.locator('text=Game in Progress!')).toBeVisible();
    });

    // --- Gameplay Simulation ---
    await test.step('Score updates are synced', async () => {
      // Host scores points (button text is "Score +10")
      await page1.click('button:has-text("Score +10")');
      
      // Verify score updates - scores are displayed in .mp-score elements
      // Wait for score to update on both screens
      await expect(page1.locator('.mp-score').first()).toHaveText('10', { timeout: 5000 });
      await expect(page2.locator('.mp-score').first()).toHaveText('10', { timeout: 5000 });
    });
  });
});
