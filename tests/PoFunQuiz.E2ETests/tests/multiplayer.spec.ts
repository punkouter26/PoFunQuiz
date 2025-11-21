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
      await expect(page1).toHaveTitle(/Multiplayer/);

      // Enter name and create game
      const nameInput = page1.locator('input[placeholder="Enter your name"]');
      await nameInput.fill('HostPlayer');
      await nameInput.blur(); // Trigger blur to ensure binding updates

      const createButton = page1.locator('button:has-text("Create New Game")');
      await expect(createButton).toBeEnabled();
      await createButton.click();

      // Wait for Game ID to appear
      await expect(page1.locator('.badge.bg-info')).toBeVisible();
      await expect(page1.locator('text=Waiting for Player 2 to join...')).toBeVisible();
    });

    // Get the Game ID
    const gameId = await page1.locator('.badge.bg-info').innerText();
    console.log(`Game ID created: ${gameId}`);
    expect(gameId).toBeTruthy();

    // --- Player 2 (Joiner) Setup ---
    await test.step('Player 2 joins the game', async () => {
      await page2.goto('/multiplayer');
      
      // Enter name and Game ID
      const nameInput = page2.locator('input[placeholder="Enter your name"]');
      await nameInput.fill('JoinPlayer');
      await nameInput.blur();

      const gameIdInput = page2.locator('input[placeholder="Enter Game ID"]');
      await gameIdInput.fill(gameId);
      await gameIdInput.blur();
      
      // Click Join
      const joinButton = page2.locator('button:has-text("Join Game")');
      await expect(joinButton).toBeEnabled();
      await joinButton.click();

      // Verify P2 sees the lobby
      await expect(page2.locator(`text=${gameId}`)).toBeVisible();
      await expect(page2.locator('text=Waiting for host to start...')).toBeVisible();
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
      // Host scores points
      await page1.click('button:has-text("Simulate Score")');
      
      // Verify score updates for Host (Player 1) on both screens
      // Note: Initial score is 0, +10 = 10.
      // We look for the score display. Based on Razor: <h1>@currentState.Player1Score</h1>
      
      // Wait for score to update
      await expect(page1.locator('.card.border-primary h1.display-4')).toHaveText('10');
      await expect(page2.locator('.card.border-primary h1.display-4')).toHaveText('10');
    });
  });
});
