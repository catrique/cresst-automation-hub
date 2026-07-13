using Microsoft.Playwright;
using AutomationApp.Services.Utils;

namespace AutomationApp.Services
{
    public class PlaywrightBrowserFactory
    {
        public static async Task<(IBrowser Browser, IBrowserContext Context)> CreateChromiumContextAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var proxyOptions = new Proxy
            {
                Server= $"http://{Program.Settings.Proxy.Host}:{Program.Settings.Proxy.Port}"
            };

            if (!string.IsNullOrWhiteSpace(Program.Settings.Proxy.Username))
            {
                proxyOptions.Username = DataProtectionService.Decrypt(Program.Settings.Proxy.Username);
                proxyOptions.Password = DataProtectionService.Decrypt(Program.Settings.Proxy.Password);
            }

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = !Program.Settings.DebugMode,
                Proxy = proxyOptions,
                Args = new[]
                {
                    "--start-maximized",
                    "--disable-extensions",
                    "--disable-popup-blocking"
                }
            };

            var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize {Width= 1920, Height = 1080},
                AcceptDownloads = true               
            };

            var context = await browser.NewContextAsync(contextOptions);
            return(browser, context);
        }
    }
}