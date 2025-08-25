using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace PoFunQuiz.Tests
{
    public class PlaywrightSampleTests
    {
        private readonly ITestOutputHelper _output;

        public PlaywrightSampleTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task HomePage_Should_Display_Title()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://localhost:5001/"); // Updated to use HTTPS port
            var title = await page.TitleAsync();
            Assert.Contains("PoFunQuiz", title);
        }

        [Fact]
        public async Task BrowseApp_InteractiveExploration()
        {
            using var playwright = await Playwright.CreateAsync();
            // Launch browser in non-headless mode for visual inspection
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
            { 
                Headless = false,
                SlowMo = 1000 // Slow down actions for better visibility
            });
            
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });
            
            var page = await context.NewPageAsync();
            
            // Navigate to the app
            await page.GotoAsync("https://localhost:5001/");
            
            // Take a screenshot
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "homepage.png" });
            _output.WriteLine("Screenshot saved: homepage.png");
            
            // Get page title and log it
            var title = await page.TitleAsync();
            _output.WriteLine($"Page title: {title}");
            
            // Get page content preview
            var bodyText = await page.Locator("body").TextContentAsync();
            var preview = bodyText?.Length > 200 ? bodyText.Substring(0, 200) : bodyText ?? "";
            _output.WriteLine($"Page content preview: {preview}...");
            
            // Look for navigation elements
            var navElements = await page.Locator("nav, .nav, [role='navigation']").AllAsync();
            _output.WriteLine($"Found {navElements.Count} navigation elements");
            
            // Look for buttons
            var buttons = await page.Locator("button").AllAsync();
            _output.WriteLine($"Found {buttons.Count} buttons");
            
            // Look for links
            var links = await page.Locator("a").AllAsync();
            _output.WriteLine($"Found {links.Count} links");
            
            // Wait a bit to keep browser open for inspection
            await page.WaitForTimeoutAsync(5000);
            
            Assert.Contains("PoFunQuiz", title);
        }
    }
}
