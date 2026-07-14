using Microsoft.Playwright;
using AutomationApp.Services.Utils;

namespace AutomationApp.Services
{
    public class PlaywrightBrowserFactory
    {
        public static async Task<(IBrowser Browser, IBrowserContext Context)> CreateChromiumContextAsync(bool headless = false)
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
                Headless = headless,
                Proxy = proxyOptions,
                Args = new[]
                {
                    "--start-maximized",
                    "--disable-extensions",
                    "--disable-popup-blocking",
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox",
                    "--disable-dev-shm-usage"
                }
            };

            var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize {Width= 1920, Height = 1080},
                AcceptDownloads = true,      
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                Locale = "pt-BR",
                TimezoneId = "America/Sao_Paulo"         
            };

            var context = await browser.NewContextAsync(contextOptions);
            return(browser, context);
        }
    }
}