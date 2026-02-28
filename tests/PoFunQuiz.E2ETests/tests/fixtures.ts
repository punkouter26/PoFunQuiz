import { test as base, expect } from '@playwright/test';

/**
 * Extended Playwright test fixture that automatically collects JavaScript console errors
 * on every test page and fails the test if any unexpected errors are found.
 *
 * Import `test` and `expect` from this file instead of `@playwright/test` to get
 * automatic JS error detection on every E2E test.
 */

// Known benign messages that should not fail tests
const IGNORED_CONSOLE_PATTERNS = [
  /\[HMR\]/, // Hot Module Reload (dev only)
  /WebSocket connection.*failed/i, // SignalR retry noise in dev
  /blazor\.web\.js/, // Internal Blazor framework messages
  /net::ERR_ABORTED/i, // Cancelled navigation requests (Blazor SPA)
  /favicon\.ico/i, // Favicon 404 is not a test concern
];

function isBenign(text: string): boolean {
  return IGNORED_CONSOLE_PATTERNS.some(p => p.test(text));
}

// Extend the base test with a `consoleErrors` fixture
const test = base.extend<{ consoleErrors: string[] }>({
  // Auto-use fixture: runs for every test automatically
  consoleErrors: [
    async ({ page }, use) => {
      const errors: string[] = [];

      // Capture browser console errors and unhandled JS exceptions
      page.on('console', msg => {
        if (msg.type() === 'error' && !isBenign(msg.text())) {
          errors.push(`[console.error] ${msg.text()}`);
        }
      });

      page.on('pageerror', err => {
        if (!isBenign(err.message)) {
          errors.push(`[pageerror] ${err.message}`);
        }
      });

      // Provide the errors array to the test (if needed for custom assertions)
      await use(errors);

      // After test: fail if any unexpected JS errors were recorded
      if (errors.length > 0) {
        throw new Error(
          `JavaScript errors detected during test:\n${errors.map(e => `  • ${e}`).join('\n')}`
        );
      }
    },
    { auto: true }, // Run for every test without needing explicit use()
  ],
});

export { test, expect };
