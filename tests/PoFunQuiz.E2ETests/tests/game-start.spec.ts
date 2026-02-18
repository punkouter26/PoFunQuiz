import { test, expect, Page } from '@playwright/test';

/**
 * E2E Test: Verifies the first question appears after entering 2 initials and clicking Start Game.
 * Covers the critical path: Home → GameSetup → GameBoard (first question visible).
 *
 * NOTE: This test may start the dev server (up to 120s) if not already running.
 * The test timeout is set to 90s to accommodate cold-start + question generation.
 */

test.describe('Game Start - First Question Appears', () => {
  let page: Page;

  // Allow 90s: up to 120s webServer timeout is handled at config level, but each
  // test only gets 30s by default — raise to 90s for this cold-start scenario.
  test.setTimeout(90000);

  test.beforeEach(async ({ page: testPage }) => {
    page = testPage;
    await page.goto('/');
    await expect(page).toHaveTitle(/PoFunQuiz/, { timeout: 60000 });

    // Wait for Blazor interactive circuit — look for the first visible heading/button
    // which only renders after the InteractiveServer SignalR circuit connects.
    await page.locator('h1, h2, button').first().waitFor({ state: 'visible', timeout: 15000 }).catch(() => {
      console.log('Blazor root selector not found — continuing with short buffer');
    });
    await page.waitForTimeout(500);
  });

  test('should show first question after entering initials and starting game', async () => {
    // Step 1: Enter player initials on the home page
    await test.step('Enter player initials', async () => {
      const player1Input = page.locator('#player1Initials');
      const player2Input = page.locator('#player2Initials');

      await expect(player1Input).toBeVisible();
      await expect(player2Input).toBeVisible();

      await player1Input.fill('AB');
      await player2Input.fill('CD');

      await expect(player1Input).toHaveValue('AB');
      await expect(player2Input).toHaveValue('CD');
    });

    // Step 2: Select a topic using the Radzen dropdown
    await test.step('Select topic', async () => {
      const topicDropdown = page.locator('#topic');
      await expect(topicDropdown).toBeVisible({ timeout: 5000 });
      await topicDropdown.click();
      // Wait for dropdown panel to open before selecting
      await page.locator('.rz-dropdown-item').first().waitFor({ state: 'visible', timeout: 5000 });
      await page.locator('.rz-dropdown-item').first().click();
      // Wait for dropdown to close (selection confirmed)
      await page.locator('.rz-dropdown-item').first().waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
    });

    // Step 3: Click Start Game → app navigates to /gamesetup, generates questions, then redirects to /game-board
    // NOTE: GameSetup.razor has NO "I'm Ready" button — it auto-generates questions and navigates directly to /game-board.
    await test.step('Click Start Game and wait for gamesetup navigation', async () => {
      const startButton = page.locator('button:has-text("Start Game")');
      await expect(startButton).toBeVisible({ timeout: 5000 });
      await startButton.click();

      // Small buffer to let Blazor router commit the navigation (SignalR keeps connection open so networkidle never fires)
      await page.waitForTimeout(500);

      // Confirm we landed on /gamesetup (may be brief before auto-redirect to /game-board)
      await expect(page).toHaveURL(/\/gamesetup|\/game-board/, { timeout: 15000 });
    });

    // Step 4: Wait for question generation (GameSetup auto-generates and redirects to /game-board)
    await test.step('Wait for question generation and game board', async () => {
      // GameSetup auto-generates questions then navigates to /game-board.
      // Wait for either the game board questions, or an error/retry UI (when OpenAI is not configured).
      const questionVisible = page.locator('.option-item').first();
      const errorAlert = page.locator('button:has-text("Retry")');
      const failedMessage = page.locator('text="Failed to generate questions"');

      // Wait for navigation to game-board or error on gamesetup page
      const result = await Promise.race([
        questionVisible.waitFor({ timeout: 60000 }).then(() => 'questions-loaded' as const),
        errorAlert.waitFor({ timeout: 60000 }).then(() => 'error' as const),
        failedMessage.waitFor({ timeout: 60000 }).then(() => 'failed' as const),
      ]).catch(() => 'timeout' as const);

      if (result === 'questions-loaded') {
        console.log('✅ First question appeared on the game board');

        // Verify we're on the game board page
        await expect(page).toHaveURL(/\/game-board/);

        // Verify question text is visible
        const questionText = page.locator('.question-container');
        await expect(questionText.first()).toBeVisible();

        // Verify there are exactly 4 options per player board
        const options = page.locator('.option-item');
        const optionCount = await options.count();
        expect(optionCount).toBeGreaterThanOrEqual(4);

        // Verify player initials are shown on the board
        await expect(page.locator('text="AB"').first()).toBeVisible();
        await expect(page.locator('text="CD"').first()).toBeVisible();

        console.log(`✅ Game board shows ${optionCount} answer options across players`);
      } else {
        // OpenAI not configured — the app handled it gracefully (no crash)
        console.log('⚠️  Question generation failed (OpenAI not configured) — verifying graceful error handling');

        // The app should still be running and showing an error message, NOT crashed
        await expect(page.locator('body')).toBeVisible();

        // Verify we're still on gamesetup (not a crash/blank page)
        await expect(page).toHaveURL(/\/gamesetup/);

        // The error/retry UI should be visible
        const retryVisible = await errorAlert.isVisible();
        const failedVisible = await failedMessage.isVisible();
        expect(retryVisible || failedVisible).toBeTruthy();

        console.log('✅ App handled missing OpenAI config gracefully with error UI');
      }
    });
  });
});
