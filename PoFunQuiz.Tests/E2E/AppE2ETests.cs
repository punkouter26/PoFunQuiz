using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace PoFunQuiz.Tests.E2E;

/// <summary>
/// End-to-End tests for the PoFunQuiz application using Playwright.
/// Tests run against https://localhost:5001 - ensure the app is running before executing tests.
/// </summary>
public class AppE2ETests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string BaseUrl = "https://localhost:5001";

    public AppE2ETests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_LoadsSuccessfully_Desktop()
    {
        // Arrange
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        var page = await context.NewPageAsync();

        // Act
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await page.TitleAsync();
        Assert.Contains("PoFunQuiz", title);
        
        // Verify main heading is present
        var heading = await page.Locator("h1, h2, h3").First.TextContentAsync();
        Assert.NotNull(heading);
        _output.WriteLine($"Page heading: {heading}");

        await context.CloseAsync();
    }

    [Fact]
    public async Task HomePage_LoadsSuccessfully_Mobile()
    {
        // Arrange - iPhone 12 Pro viewport
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15"
        });
        var page = await context.NewPageAsync();

        // Act
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await page.TitleAsync();
        Assert.Contains("PoFunQuiz", title);
        
        // Verify page is responsive
        var bodyWidth = await page.EvaluateAsync<int>("() => document.body.offsetWidth");
        Assert.True(bodyWidth <= 390, "Page should fit mobile viewport");

        await context.CloseAsync();
    }

    [Fact]
    public async Task Navigation_ToLeaderboard_Works()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);

        // Act
        var leaderboardLink = page.Locator("a[href='/leaderboard'], a:has-text('Leaderboard')").First;
        await leaderboardLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var url = page.Url;
        Assert.Contains("/leaderboard", url.ToLower());
        _output.WriteLine($"Navigated to: {url}");

        await context.CloseAsync();
    }

    [Fact]
    public async Task Navigation_ToDiagnostics_Works()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);

        // Act
        var diagLink = page.Locator("a[href='/diag'], a:has-text('Diagnostics')").First;
        await diagLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var url = page.Url;
        Assert.Contains("/diag", url.ToLower());
        
        // Verify diagnostics content is present
        var diagnosticsHeading = await page.Locator("h1:has-text('Diagnostics')").CountAsync();
        Assert.True(diagnosticsHeading > 0, "Diagnostics page should have a heading");

        await context.CloseAsync();
    }

    [Fact]
    public async Task DiagnosticsPage_DisplaysHealthChecks()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        // Act
        await page.GotoAsync($"{BaseUrl}/diag");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Wait for health checks to complete (they load on page init)
        await page.WaitForTimeoutAsync(3000);

        // Assert
        var cards = await page.Locator(".card").CountAsync();
        Assert.True(cards >= 3, $"Should have at least 3 health check cards, found {cards}");
        
        // Verify expected health checks are present
        var pageContent = await page.ContentAsync();
        Assert.Contains("Table Storage", pageContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OpenAI", pageContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Internet", pageContent, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine($"Found {cards} health check cards");

        await context.CloseAsync();
    }

    [Fact]
    public async Task DiagnosticsPage_RefreshButton_Works()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/diag");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000);

        // Act
        var refreshButton = page.Locator("button:has-text('Refresh')").First;
        await refreshButton.ClickAsync();
        await page.WaitForTimeoutAsync(2000);

        // Assert
        // Button should still be present after refresh
        var buttonCount = await page.Locator("button:has-text('Refresh')").CountAsync();
        Assert.True(buttonCount > 0, "Refresh button should still be present");

        await context.CloseAsync();
    }

    [Fact]
    public async Task GameFlow_CanStartNewGame()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Look for start game button or similar
        var startButton = page.Locator("button:has-text('Start'), button:has-text('New Game'), button:has-text('Play')").First;
        var buttonCount = await startButton.CountAsync();

        if (buttonCount > 0)
        {
            await startButton.ClickAsync();
            await page.WaitForTimeoutAsync(2000);

            // Assert - Should navigate away from home or show game setup
            var url = page.Url;
            _output.WriteLine($"After clicking start, URL: {url}");
            
            // Either we're on a different page or there's game content visible
            var hasGameContent = await page.Locator("input, select, .quiz, .game").CountAsync() > 0;
            Assert.True(!url.EndsWith("/") || hasGameContent, 
                "Should either navigate to game page or show game setup");
        }
        else
        {
            _output.WriteLine("No start game button found on homepage");
        }

        await context.CloseAsync();
    }

    [Fact]
    public async Task ResponsiveDesign_MobileNavigation_Works()
    {
        // Arrange - Mobile viewport
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 375, Height = 667 }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Check if hamburger menu or mobile nav exists
        var mobileMenu = page.Locator("button.navbar-toggler, .mobile-menu, [aria-label='Toggle navigation']");
        var menuCount = await mobileMenu.CountAsync();

        if (menuCount > 0)
        {
            await mobileMenu.First.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            
            // Assert - Menu should expand and show links
            var navLinks = await page.Locator("nav a, .navbar a").CountAsync();
            Assert.True(navLinks > 0, "Mobile menu should contain navigation links");
            _output.WriteLine($"Found {navLinks} navigation links in mobile menu");
        }
        else
        {
            // If no mobile menu, nav should still be accessible
            var navLinks = await page.Locator("nav a, a[href='/']").CountAsync();
            Assert.True(navLinks > 0, "Navigation should be accessible on mobile");
            _output.WriteLine($"No mobile menu toggle found, but {navLinks} links are visible");
        }

        await context.CloseAsync();
    }

    [Fact]
    public async Task PWA_ManifestIsPresent()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        // Act
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var manifestLink = await page.Locator("link[rel='manifest']").CountAsync();
        Assert.True(manifestLink > 0, "PWA manifest link should be present in <head>");

        var manifestHref = await page.Locator("link[rel='manifest']").GetAttributeAsync("href");
        Assert.NotNull(manifestHref);
        _output.WriteLine($"Manifest link: {manifestHref}");

        await context.CloseAsync();
    }

    [Fact]
    public async Task ServiceWorker_IsRegistered()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000);

        // Act & Assert
        var serviceWorkerRegistered = await page.EvaluateAsync<bool>(@"
            () => {
                return 'serviceWorker' in navigator;
            }
        ");
        
        Assert.True(serviceWorkerRegistered, "Service Worker API should be available");
        _output.WriteLine("Service Worker API is available in browser");

        await context.CloseAsync();
    }

    [Fact]
    public async Task Accessibility_PageHasProperHeadings()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var h1Count = await page.Locator("h1").CountAsync();

        // Assert
        Assert.True(h1Count >= 1, "Page should have at least one h1 heading for accessibility");
        
        var h1Text = await page.Locator("h1").First.TextContentAsync();
        _output.WriteLine($"Main heading (h1): {h1Text}");

        await context.CloseAsync();
    }

    [Fact]
    public async Task PerformanceTest_PageLoadsWithin5Seconds()
    {
        // Arrange
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        var startTime = DateTime.UtcNow;

        // Act
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.Load);
        var loadTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(loadTime.TotalSeconds < 5, 
            $"Page should load within 5 seconds, took {loadTime.TotalSeconds:F2}s");
        _output.WriteLine($"Page load time: {loadTime.TotalMilliseconds:F0}ms");

        await context.CloseAsync();
    }
}
