using Microsoft.Playwright;

namespace AutomationApp.Services.Betha;

public class BethaUpdateTokenService : BethaBaseService
{
    private readonly Action<string> _logger;

    private const string FrameId = "#novosocFrame";

    public BethaUpdateTokenService(Action<string> logger)
    {
        _logger = logger;
    }

    public async Task<string> UpdateToken()
    {
        try
        {
            _logger("Realizando login no Betha");
            await InitializeAndLoginAsync();

            _logger("Acessando a páginad do RH");
            return await NavigateToRhAndGetTokenAsync();

        }
        finally
        {
            _logger("Encerrando sessão");
            await CloseAsync();
        }
    }
}