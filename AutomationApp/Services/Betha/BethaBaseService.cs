using Microsoft.Playwright;
using AutomationApp.Services.Utils;

namespace AutomationApp.Services.Betha
{
    public abstract class BethaBaseService
    {
        protected IBrowser? _browser;
        protected IBrowserContext? _context;
        protected IPage? _page;

        public async Task InitializeAndLoginAsync()
        {
            var (browser, context) = await PlaywrightBrowserFactory.CreateChromiumContextAsync(true);
            _browser = browser;
            _context = context;
            _page = await _context.NewPageAsync();

            await _page.GotoAsync(Program.Settings.Betha.Api.LoginUrl);

            string username = DataProtectionService.Decrypt(Program.Settings.Betha.Credentials.Login);
            string password = DataProtectionService.Decrypt(Program.Settings.Betha.Credentials.Password);

            await _page.FillAsync("#login\\:iUsuarios", username);
            await _page.FillAsync("#login\\:senha", password);
            await _page.ClickAsync("#login\\:btAcessar");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        public async Task<string> NavigateToRhAndGetTokenAsync()
        {
            if (!Program.Settings.Betha.Api.Endpoints.TryGetValue("Token", out string? endp) || string.IsNullOrEmpty(endp))
                throw new InvalidOperationException("O endpoint 'Token' não foi localizado nas configurações do Betha.");

            if (_page == null)
                throw new InvalidOperationException("A página do navegador não foi inicializada.");

            string? bearerToken = null;
            var tokenSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<IRequest> requestHandler = (sender, request) =>
            {
                if (tokenSource.Task.IsCompleted) return;

                try
                {
                    var headers = request.Headers;
                    if (headers.TryGetValue("authorization", out string? authHeader) && !string.IsNullOrEmpty(authHeader))
                    {
                        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            string token = authHeader.Substring(7).Trim();
                            tokenSource.TrySetResult(token);
                        }
                    }
                }
                catch
                {
                }
            };

            _page.Request += requestHandler;

            try
            {
                await _page.GotoAsync(endp);
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await _page.ReloadAsync();

                var timeoutTask = Task.Delay(15000);
                var completedTask = await Task.WhenAny(tokenSource.Task, timeoutTask);

                if (completedTask == tokenSource.Task)
                    bearerToken = await tokenSource.Task;
                else
                    throw new TimeoutException("Tempo limite esgotado. Nenhuma requisição contendo o cabeçalho 'Authorization' (Bearer) foi detectada após o recarregamento.");
            }
            finally
            {
                _page.Request -= requestHandler;
            }

            return bearerToken;
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