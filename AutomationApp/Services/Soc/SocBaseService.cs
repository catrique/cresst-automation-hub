using Microsoft.Playwright;
using AutomationApp.Services.Utils;

namespace AutomationApp.Services.Soc
{
    public abstract class SocBaseService
    {
        protected IBrowser? _browser;
        protected IBrowserContext? _context;
        protected IPage? _page;

        public async Task InitializeAndLoginAsync()
        {
            var (browser, context) = await PlaywrightBrowserFactory.CreateChromiumContextAsync();
            _browser = browser;
            _context = context;
            _page = await _context.NewPageAsync();

            await _page.GotoAsync(Program.Settings.Soc.BaseUrl);

            string username = DataProtectionService.Decrypt(Program.Settings.Soc.Credentials.Login);
            string password = DataProtectionService.Decrypt(Program.Settings.Soc.Credentials.Password);
            string virtualPassword = DataProtectionService.Decrypt(Program.Settings.Soc.Credentials.VirtualPassword);

            await _page.WaitForSelectorAsync("#bt_entrar");
            await _page.FillAsync("#usu", username);
            await _page.FillAsync("#senha", password);

            try { await _page.ClickAsync("#empsoc"); } catch { }

            await _page.WaitForSelectorAsync("#teclado");

            foreach (char digito in virtualPassword)
            {
                if (digito != ' ' && digito != ',')
                {
                    var botaoTeclado = _page.Locator($"//div[@id='teclado']//input[@value='{digito}']");
                    if (await botaoTeclado.CountAsync() > 0)
                    {
                        await botaoTeclado.First.ClickAsync();
                        await Task.Delay(300);
                    }
                }
            }

            await _page.ClickAsync("#bt_entrar");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        }

        public async Task NavigateToScreenCodeAsync(string screenCode)
        {
            if (_page == null) throw new InvalidOperationException("Browser not initialized.");

            string menuTriggerSelector = "a.sidenav-trigger";
            await _page.WaitForSelectorAsync(menuTriggerSelector);
            await _page.ClickAsync(menuTriggerSelector);

            string searchSelector = "#ipt-text-busca-programa-menu";
            await _page.WaitForSelectorAsync(searchSelector);

            await _page.FillAsync(searchSelector, screenCode);
            await _page.Keyboard.PressAsync("Enter");

            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);
        }

        public async Task CloseAsync()
        {
            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
        }

        public IPage? GetPage()
        {
            return _page;
        }
    }
}