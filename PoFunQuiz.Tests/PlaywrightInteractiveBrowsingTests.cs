using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace PoFunQuiz.Tests
{
    public class PlaywrightInteractiveBrowsingTests
    {
        private readonly ITestOutputHelper _output;

        public PlaywrightInteractiveBrowsingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task BrowseApp_FullInteractiveSession()
        {
            using var playwright = await Playwright.CreateAsync();

            // Launch browser with slower interactions for better visibility
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,  // Show the browser
                SlowMo = 500,      // Slow down actions
                Args = new[] { "--start-maximized" }  // Start maximized
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = await context.NewPageAsync();

            // Enable console logging
            page.Console += (_, e) => _output.WriteLine($"Console {e.Type}: {e.Text}");

            try
            {
                _output.WriteLine("🚀 Starting PoFunQuiz exploration...");

                // Navigate to the app
                await page.GotoAsync("https://localhost:5001/");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); // Less strict than NetworkIdle

                // Take initial screenshot
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "app-homepage.png", FullPage = true });
                _output.WriteLine("📸 Screenshot saved: app-homepage.png");

                // Get basic page info
                var title = await page.TitleAsync();
                var url = page.Url;
                _output.WriteLine($"📄 Page Title: {title}");
                _output.WriteLine($"🔗 Current URL: {url}");

                // Check for common UI elements
                await ExploreUIElements(page);

                // Look for and interact with navigation
                await ExploreNavigation(page);

                // Look for interactive elements
                await ExploreInteractiveElements(page);

                // Take final screenshot
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "app-final-state.png", FullPage = true });
                _output.WriteLine("📸 Final screenshot saved: app-final-state.png");

                // Keep browser open for manual inspection
                _output.WriteLine("🔍 Browser will stay open for 10 seconds for manual inspection...");
                await page.WaitForTimeoutAsync(10000);

            }
            catch (System.Exception ex)
            {
                _output.WriteLine($"❌ Error during browsing: {ex.Message}");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = "app-error-state.png" });
                throw;
            }
        }

        private async Task ExploreUIElements(IPage page)
        {
            _output.WriteLine("\n🔍 Exploring UI Elements:");

            // Check for main content areas
            var headers = await page.Locator("h1, h2, h3, h4, h5, h6").AllAsync();
            _output.WriteLine($"   📝 Found {headers.Count} headers");

            for (int i = 0; i < Math.Min(headers.Count, 5); i++)
            {
                var headerText = await headers[i].TextContentAsync();
                _output.WriteLine($"      • {headerText?.Trim()}");
            }

            // Check for buttons
            var buttons = await page.Locator("button").AllAsync();
            _output.WriteLine($"   🔘 Found {buttons.Count} buttons");

            // Check for forms
            var forms = await page.Locator("form").AllAsync();
            _output.WriteLine($"   📋 Found {forms.Count} forms");

            // Check for inputs
            var inputs = await page.Locator("input").AllAsync();
            _output.WriteLine($"   ⌨️  Found {inputs.Count} input fields");
        }

        private async Task ExploreNavigation(IPage page)
        {
            _output.WriteLine("\n🧭 Exploring Navigation:");

            var navLinks = await page.Locator("nav a, .nav a, .navbar a").AllAsync();
            _output.WriteLine($"   🔗 Found {navLinks.Count} navigation links");

            for (int i = 0; i < Math.Min(navLinks.Count, 5); i++)
            {
                var linkText = await navLinks[i].TextContentAsync();
                var href = await navLinks[i].GetAttributeAsync("href");
                _output.WriteLine($"      • {linkText?.Trim()} → {href}");
            }
        }

        private async Task ExploreInteractiveElements(IPage page)
        {
            _output.WriteLine("\n🎮 Exploring Interactive Elements:");

            // Look for clickable elements
            var clickableElements = await page.Locator("button:visible, a:visible, [onclick]:visible").AllAsync();
            _output.WriteLine($"   👆 Found {clickableElements.Count} clickable elements");

            // Try clicking the first few buttons if they exist
            var visibleButtons = await page.Locator("button:visible").AllAsync();
            if (visibleButtons.Count > 0)
            {
                _output.WriteLine("   🎯 Attempting to interact with buttons:");

                for (int i = 0; i < Math.Min(visibleButtons.Count, 3); i++)
                {
                    try
                    {
                        var buttonText = await visibleButtons[i].TextContentAsync();
                        _output.WriteLine($"      • Clicking button: {buttonText?.Trim()}");

                        await visibleButtons[i].ClickAsync();
                        await page.WaitForTimeoutAsync(1000); // Wait to see result

                        // Take screenshot after interaction
                        await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"app-after-button-{i}.png" });

                    }
                    catch (System.Exception ex)
                    {
                        _output.WriteLine($"      ⚠️  Could not click button {i}: {ex.Message}");
                    }
                }
            }
        }
    }
}
