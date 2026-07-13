using Microsoft.Playwright;
using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc
{
    public class SocAppointmentWebAutomationService : SocBaseService
    {
        private readonly Action<string> _logger;
        private const string FrameId = "#novosocFrame";

        public SocAppointmentWebAutomationService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAppointmentAutomationAsync(List<SocAppointmentData> appointments, string tipoCompromissoValor, IPage? externalPage = null)
        {
            if (appointments == null || appointments.Count == 0)
            {
                _logger("⚠️ Nenhum agendamento pendente para processar.");
                return;
            }

            bool reusandoSessao = externalPage != null;

            try
            {
                if (reusandoSessao)
                {
                    _logger("🔄 Reutilizando a sessão aberta do cadastro de funcionários (Sem necessidade de login)...");
                    _page = externalPage;
                }
                else
                {
                    _logger("🚀 Iniciando novo browser e autenticando no SOC para agendamentos...");
                    await InitializeAndLoginAsync();
                }

                _logger("📺 Navegando para a tela de Agendamento de Compromissos (Programa 236)...");
                await NavigateToScreenCodeAsync("236");

                var frame = _page?.FrameLocator(FrameId);
                if (frame == null) throw new InvalidOperationException("Frame '#novosocFrame' não localizado.");

                foreach (var app in appointments)
                {
                    _logger($"\n📅 [AGENDANDO] Linha {app.LinhaPlanilha} | Funcionário: {app.Nome} (CPF: {app.Cpf})");

                    try
                    {
                        var btnIncluir = frame.Locator("a[href*=\"doAcao('inc')\"]");
                        await btnIncluir.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                        await btnIncluir.ClickAsync();
                        await Task.Delay(2000);

                        var selectTipo = frame.Locator("#tipoCompromisso");
                        await selectTipo.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                        await selectTipo.SelectOptionAsync(new[] { tipoCompromissoValor });
                        await Task.Delay(1500);

                        var campoCpfBusca = frame.Locator("#cpfBusca");
                        if (await campoCpfBusca.IsVisibleAsync())
                        {
                            await campoCpfBusca.FillAsync(app.Cpf);
                            await _page.Keyboard.PressAsync("Enter");
                            await Task.Delay(2000);
                        }


                        _logger("  💾 A gravar o agendamento...");
                        var btnGravar = frame.Locator("a[href*=\"doAcao('sav')\"], a[href*=\"doAcao('save')\"], img[src*='confirma.png']").First;
                        await btnGravar.ScrollIntoViewIfNeededAsync();
                        await btnGravar.ClickAsync();
                        await Task.Delay(3000);

                        _logger($"  ✅ Sucesso! Agendamento concluído para o funcionário.");
                        app.StatusProcessamento = "AGENDADO";
                        app.MotivoStatus = "Sucesso";
                    }
                    catch (Exception ex)
                    {
                        _logger($"  ❌ Erro no agendamento do CPF {app.Cpf}: {ex.Message}");
                        app.StatusProcessamento = "ERRO_WEB";
                        app.MotivoStatus = ex.Message;
                        
                        try 
                        {
                            var btnCancelar = frame.Locator("a[href*=\"doAcao('can')\"] | img[src*='cancela.png']").First;
                            if (await btnCancelar.IsVisibleAsync()) await btnCancelar.ClickAsync();
                        } 
                        catch {}
                    }
                }
            }
            catch (Exception ex)
            {
                _logger($"❌ Erro crítico no motor do agendamento: {ex.Message}");
            }
            finally
            {
                if (!reusandoSessao)
                    await CloseAsync();
            }
        }
    }
}