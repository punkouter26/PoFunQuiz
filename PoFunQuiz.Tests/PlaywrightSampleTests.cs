using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace PoFunQuiz.Tests
{
    public class PlaywrightSampleTests
    {
        [Fact]
        public async Task HomePage_Should_Display_Title()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://localhost:5000/"); // Adjust port if needed
            var title = await page.TitleAsync();
            Assert.Contains("PoFunQuiz", title);
        }
    }
}
