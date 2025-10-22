# PoFunQuiz E2E Tests

End-to-end tests for PoFunQuiz using Playwright and TypeScript.

## Prerequisites

- Node.js 18+ installed
- .NET 9.0 SDK installed
- PoFunQuiz application built

## Setup

```bash
# Install dependencies
npm install

# Install Playwright browsers
npx playwright install chromium
```

## Running Tests

```bash
# Run all tests (headless)
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

## Test Coverage

### Complete Game Flow
- Player name entry validation
- Topic selection
- Game start process
- Question loading and answering
- Multi-player turn management
- Results page display
- Leaderboard navigation

### Edge Cases
- Validation errors (missing players/topic)
- Single player mode
- Responsive mobile layout
- API health checks

## Test Structure

```
PoFunQuiz.E2ETests/
├── tests/
│   └── game-flow.spec.ts    # Main E2E test suite
├── playwright.config.ts      # Playwright configuration
├── tsconfig.json            # TypeScript configuration
└── package.json             # Node dependencies
```

## Configuration

The tests automatically start the PoFunQuiz server on `http://localhost:5000`.

To run against a different URL:
```bash
BASE_URL=https://pofunquiz.azurewebsites.net npm test
```

## CI/CD Integration

In GitHub Actions, the tests will:
- Run with 2 retries
- Generate HTML reports
- Capture screenshots and videos on failure

## Notes

- Tests gracefully handle OpenAI API unavailability in local development
- Mobile responsive tests verify layout at 375x667 viewport
- API health checks verify server readiness
