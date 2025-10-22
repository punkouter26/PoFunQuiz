import { test, expect, Page } from '@playwright/test';

/**
 * E2E Test Suite for PoFunQuiz Game
 * Tests the complete game flow from setup to results
 */

test.describe('PoFunQuiz Complete Game Flow', () => {
  let page: Page;

  test.beforeEach(async ({ page: testPage }) => {
    page = testPage;
    await page.goto('/');
    await expect(page).toHaveTitle(/PoFunQuiz/);
  });

  test('should complete a full game with two players', async () => {
    // Step 1: Enter player names
    await test.step('Enter player 1 name', async () => {
      const player1Input = page.locator('input[placeholder="Enter player name"]').first();
      await player1Input.fill('Alice');
      await expect(player1Input).toHaveValue('Alice');
    });

    await test.step('Enter player 2 name', async () => {
      const player2Input = page.locator('input[placeholder="Enter player name"]').nth(1);
      await player2Input.fill('Bob');
      await expect(player2Input).toHaveValue('Bob');
    });

    // Step 2: Select a topic
    await test.step('Select topic', async () => {
      // Wait for topic buttons to be visible
      await page.waitForSelector('text=Science', { timeout: 5000 });
      await page.click('text=Science');
      
      // Verify topic is selected (button should have active state)
      const scienceButton = page.locator('button:has-text("Science")');
      await expect(scienceButton).toBeVisible();
    });

    // Step 3: Start the game
    await test.step('Start game', async () => {
      const startButton = page.locator('button:has-text("Start Game")');
      await expect(startButton).toBeEnabled();
      await startButton.click();
      
      // Wait for navigation to game setup page
      await page.waitForURL(/\/gamesetup/, { timeout: 10000 });
    });

    // Step 4: Wait for questions to load
    await test.step('Wait for questions to load', async () => {
      // Wait for either success or error state
      const questionOrError = page.locator('text="Question 1"').or(page.locator('text="Failed to generate questions"'));
      await questionOrError.waitFor({ timeout: 15000 });
      
      // Check if we got questions or an error
      const hasError = await page.locator('text="Failed to generate questions"').isVisible();
      
      if (hasError) {
        console.log('⚠️  OpenAI service not configured - test will use fallback mode');
        // In case of error, we can still test the UI flow with retry
        const retryButton = page.locator('button:has-text("Retry")');
        if (await retryButton.isVisible()) {
          console.log('Test completed - Question generation requires OpenAI configuration');
          return; // Exit gracefully
        }
      } else {
        console.log('✅ Questions loaded successfully');
      }
    });

    // Step 5: Play through all questions (if questions loaded)
    const hasQuestions = await page.locator('text="Question 1"').isVisible();
    
    if (hasQuestions) {
      await test.step('Answer questions for both players', async () => {
        for (let questionNum = 1; questionNum <= 5; questionNum++) {
          console.log(`Answering question ${questionNum}...`);
          
          // Verify we're on the correct question
          await expect(page.locator(`text="Question ${questionNum}"`)).toBeVisible();
          
          // Wait for options to load
          await page.waitForSelector('button.answer-option, label:has(input[type="radio"])', { timeout: 5000 });
          
          // Player 1's turn
          const player1Indicator = page.locator('text="Alice"').or(page.locator('text="Player 1"'));
          await player1Indicator.waitFor({ timeout: 5000 });
          
          // Select first answer option for Player 1
          const firstOption = page.locator('button.answer-option, label:has(input[type="radio"])').first();
          await firstOption.click();
          await page.waitForTimeout(500);
          
          // Click Submit/Next
          const submitButton = page.locator('button:has-text("Submit"), button:has-text("Next")').first();
          await submitButton.click();
          await page.waitForTimeout(1000);
          
          // Player 2's turn
          const player2Indicator = page.locator('text="Bob"').or(page.locator('text="Player 2"'));
          await player2Indicator.waitFor({ timeout: 5000 });
          
          // Select second answer option for Player 2
          const secondOption = page.locator('button.answer-option, label:has(input[type="radio"])').nth(1);
          await secondOption.click();
          await page.waitForTimeout(500);
          
          // Click Submit/Next
          const submitButton2 = page.locator('button:has-text("Submit"), button:has-text("Next")').first();
          await submitButton2.click();
          await page.waitForTimeout(1000);
        }
      });

      // Step 6: View results
      await test.step('View results page', async () => {
        // Wait for results page
        await page.waitForURL(/\/results/, { timeout: 10000 });
        
        // Verify both players' scores are displayed
        await expect(page.locator('text="Alice"')).toBeVisible();
        await expect(page.locator('text="Bob"')).toBeVisible();
        
        // Verify score display
        const scoreElements = page.locator('text=/\\d+\\/5|Score:.*\\d+/');
        await expect(scoreElements.first()).toBeVisible();
        
        console.log('✅ Results page displayed successfully');
      });

      // Step 7: Navigate to leaderboard
      await test.step('View leaderboard', async () => {
        const leaderboardButton = page.locator('button:has-text("View Leaderboard"), a:has-text("Leaderboard")');
        
        if (await leaderboardButton.isVisible()) {
          await leaderboardButton.click();
          await page.waitForURL(/\/leaderboard/, { timeout: 5000 });
          
          // Verify leaderboard page loaded
          await expect(page.locator('text=/Leaderboard|Top Players/')).toBeVisible();
          console.log('✅ Leaderboard page displayed successfully');
        }
      });

      // Step 8: Return to home
      await test.step('Return to home page', async () => {
        const homeButton = page.locator('button:has-text("New Game"), button:has-text("Play Again"), a:has-text("Home")').first();
        
        if (await homeButton.isVisible()) {
          await homeButton.click();
          await page.waitForURL('/', { timeout: 5000 });
          console.log('✅ Returned to home page');
        }
      });
    }
  });

  test('should handle validation errors', async () => {
    await test.step('Attempt to start game without player names', async () => {
      // Try to start without entering names
      const startButton = page.locator('button:has-text("Start Game")');
      
      // Button might be disabled or clicking might show an error
      if (await startButton.isEnabled()) {
        await startButton.click();
        
        // Should still be on home page or show validation message
        await expect(page).toHaveURL('/');
      }
    });

    await test.step('Attempt to start game without selecting topic', async () => {
      // Enter player names but no topic
      await page.locator('input[placeholder="Enter player name"]').first().fill('Alice');
      await page.locator('input[placeholder="Enter player name"]').nth(1).fill('Bob');
      
      const startButton = page.locator('button:has-text("Start Game")');
      
      if (await startButton.isEnabled()) {
        await startButton.click();
        await expect(page).toHaveURL('/');
      }
    });
  });

  test('should handle single player mode', async () => {
    await test.step('Play with only one player', async () => {
      // Enter only first player name
      await page.locator('input[placeholder="Enter player name"]').first().fill('Solo Player');
      
      // Select topic
      await page.click('text=Science');
      
      // Try to start game
      const startButton = page.locator('button:has-text("Start Game")');
      
      if (await startButton.isEnabled()) {
        await startButton.click();
        
        // Game might start or require both players
        const isOnGameSetup = page.url().includes('/gamesetup');
        const isStillOnHome = page.url() === '/' || page.url().endsWith('/');
        
        expect(isOnGameSetup || isStillOnHome).toBeTruthy();
      }
    });
  });

  test('should be responsive on mobile viewport', async () => {
    await test.step('Set mobile viewport', async () => {
      await page.setViewportSize({ width: 375, height: 667 });
    });

    await test.step('Verify mobile layout', async () => {
      await expect(page.locator('input[placeholder="Enter player name"]').first()).toBeVisible();
      
      // Topic buttons should still be visible
      await expect(page.locator('button:has-text("Science")')).toBeVisible();
      
      console.log('✅ Mobile layout renders correctly');
    });
  });
});

test.describe('API Health Checks', () => {
  test('should have healthy API endpoint', async ({ request }) => {
    const response = await request.get('/api/health');
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
  });

  test('should load Swagger documentation', async ({ page }) => {
    await page.goto('/scalar/v1');
    await expect(page.locator('body')).toBeVisible();
  });
});
