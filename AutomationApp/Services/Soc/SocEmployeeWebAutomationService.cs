using Microsoft.Playwright;
using AutomationApp.Models.Soc;
using AutomationApp.Utils;

namespace AutomationApp.Services.Soc
{
    public class SocEmployeeWebAutomationService : SocBaseService
    {
        private readonly Action<string> _logger;
        private const string FrameId = "#novosocFrame";

        public SocEmployeeWebAutomationService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteWebAutomationAsync(List<SocEmployeeData> employees, string setorPlanilhaBase)
        {
            if (employees == null || employees.Count == 0)
            {
                _logger("⚠️ Nenhum funcionário pendente para inclusão.");
                return;
            }

            string setorPadraoFallback = string.IsNullOrWhiteSpace(setorPlanilhaBase) ? "EDUCAÇÃO" : setorPlanilhaBase.Trim().ToUpper();

            try
            {
                await InitializeAndLoginAsync();
                await NavigateToScreenCodeAsync("232");

                var frame = _page?.FrameLocator(FrameId);
                if (frame == null) throw new InvalidOperationException("Frame '#novosocFrame' não localizado.");

                var cargosAdministrativos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ASSISTENTE EDUCACIONAL",
                    "TECNICO ESCOLAR",
                    "TÉCNICO ESCOLAR",
                    "SUPERVISOR ORIENTADOR DE ENSINO"
                };

                foreach (var emp in employees)
                {
                    _logger($"\n👤 [INICIANDO] {emp.Nome}");

                    try
                    {
                        string setorParaSelecionar = cargosAdministrativos.Contains(emp.Cargo) ? "ADMINISTRATIVO" : setorPadraoFallback;

                        string textoLotacao = emp.Lotacao;

                        var btnIncluir = frame.Locator("a[href*=\"doAcao('inc')\"]");
                        await btnIncluir.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                        await btnIncluir.ClickAsync();
                        await Task.Delay(2000);

                        await SelectDropdownNormalizedAsync(frame, "#csitefiltrado", "PREFEITURA MUNICIPAL DE DIVINÓPOLIS", "Empresa Principal");
                        await Task.Delay(1500);

                        await SelectDropdownNormalizedAsync(frame, "#codigoSetorAjax", setorParaSelecionar, "Setor");
                        await Task.Delay(1500);

                        bool cargoEncontrado = await SelectDropdownNormalizedAsync(frame, "#codigoCargoAjax", emp.Cargo, "Cargo");
                        if (!cargoEncontrado)
                        {
                            await CancelarFormularioAsync(frame);
                            emp.StatusProcessamento = "ERRO_WEB";
                            emp.MotivoStatus = "Cargo não encontrado";
                            continue;
                        }
                        await Task.Delay(1500);

                        bool lotacaoEncontrada = await SelectDropdownNormalizedAsync(frame, "#codigoSiteAjax", textoLotacao, "Lotação", "SEMED - SECRETARIA MUNICIPAL DE EDUCAÇÃO");
                        if (!lotacaoEncontrada)
                        {
                            await CancelarFormularioAsync(frame);
                            emp.StatusProcessamento = "ERRO_WEB";
                            emp.MotivoStatus = "Lotação não encontrada";
                            continue;
                        }
                        await Task.Delay(1500);

                        await frame.Locator("#nomeFuncionario").FillAsync(emp.Nome);
                        _logger($"  ✓ Nome: {emp.Nome}");

                        await frame.Locator("#dataNascimentoFormatada").FillAsync(emp.DataNascimento);
                        await frame.Locator("#dataAdmissao").FillAsync(emp.DataAdmissao);

                        var campoCpf = frame.Locator("#cpf");
                        await campoCpf.FocusAsync();
                        await campoCpf.ClearAsync();
                        await _page.Keyboard.TypeAsync(emp.Cpf);
                        await _page.Keyboard.PressAsync("Tab");
                        _logger($"  ✓ CPF preenchido: {emp.Cpf}");
                        await Task.Delay(500);

                        string valorSexo = !string.IsNullOrWhiteSpace(emp.Sexo) && emp.Sexo.Equals("MASCULINO", StringComparison.OrdinalIgnoreCase) ? "1" : "2";
                        await frame.Locator("#sexoFuncionario").SelectOptionAsync(new[] { valorSexo });

                        await frame.Locator("#matriculaFuncionario").FillAsync(emp.MatriculaAnterior);
                        _logger($"  ✓ Matrícula: {emp.MatriculaAnterior}");

                        if (!string.IsNullOrWhiteSpace(emp.Email))
                        {
                            try { await frame.Locator("#emailPessoal").FillAsync(emp.Email.ToLower().Trim()); } catch { }
                            try { await frame.Locator("#emailCorporativo").FillAsync(emp.Email.ToLower().Trim()); } catch { }
                            _logger($"  ✓ E-mail: {emp.Email}");
                        }

                        _logger("  🔍 Abrindo filtro de Categoria eSocial...");
                        var iconeBusca = frame.Locator("#iconeAbrirFiltroCategoria");
                        await iconeBusca.ScrollIntoViewIfNeededAsync();
                        await iconeBusca.ClickAsync();

                        var campoBuscaModal = frame.Locator("#inputFiltroCategoria");
                        await campoBuscaModal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                        await campoBuscaModal.ClearAsync();
                        await campoBuscaModal.FillAsync("306");
                        await _page.Keyboard.PressAsync("Enter");
                        await Task.Delay(2000);

                        var targetCellESocial = frame.Locator("td[data-codigo='306']").First;
                        await targetCellESocial.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                        await targetCellESocial.ClickAsync();
                        _logger("  ✓ Categoria eSocial 306 selecionada");
                        await Task.Delay(1500);

                        _logger("  💾 Clicando no botão Salvar/Gravar...");
                        var btnGravar = frame.Locator("a[href*=\"doAcao('sav')\"], a[href*=\"doAcao('save')\"], #btnGravar, #gravar, img[src*='confirma.png']").First;
                        await btnGravar.ScrollIntoViewIfNeededAsync();
                        await btnGravar.ClickAsync();
                        await Task.Delay(4000);

                        bool formAindaAberto = await btnGravar.IsVisibleAsync();
                        if (!formAindaAberto)
                        {
                            _logger($"  ✅ Sucesso! {emp.Nome} cadastrado e salvo.");
                            emp.StatusProcessamento = "CADASTRADO";
                            emp.MotivoStatus = "Sucesso";
                        }
                        else
                        {
                            _logger("  ❌ Erro ao gravar. Formulário permaneceu aberto na tela.");
                            await CancelarFormularioAsync(frame);
                            emp.StatusProcessamento = "ERRO_WEB";
                            emp.MotivoStatus = "Erro ao gravar. Formulário permaneceu aberto.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger($"  ❌ Exceção: {ex.Message}");
                        emp.StatusProcessamento = "ERRO_WEB";
                        emp.MotivoStatus = ex.Message;
                        await CancelarFormularioAsync(frame);
                    }
                }
            }
            finally
            {
                  MessageConsole.Success($"\n---Cadastro Concluidos!---\n");
            }
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            string cleanText = text.Replace("\u00A0", " ").Replace("&nbsp;", " ");
            string normalized = cleanText.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (char c in normalized)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToUpper().Trim();
        }

        private async Task<bool> SelectDropdownNormalizedAsync(IFrameLocator frame, string selector, string searchText, string fieldName, string fallbackText = null)
        {
            try
            {
                var selectLocator = frame.Locator(selector);
                await selectLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

                var options = selectLocator.Locator("option");
                int count = await options.CountAsync();
                string searchNorm = NormalizeText(searchText);

                if (!string.IsNullOrEmpty(searchNorm))
                {
                    for (int i = 0; i < count; i++)
                    {
                        string text = await options.Nth(i).TextContentAsync() ?? "";
                        if (NormalizeText(text) == searchNorm)
                        {
                            string val = await options.Nth(i).GetAttributeAsync("value") ?? "";
                            await selectLocator.SelectOptionAsync(new[] { val });
                            _logger($"  ✓ {fieldName}: {text.Replace("\u00A0", " ").Trim()}");
                            return true;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(fallbackText))
                {
                    _logger($"  ⚠ {fieldName} '{searchText}' não encontrado ou em branco. Tentando padrão...");
                    string fallbackNorm = NormalizeText(fallbackText);

                    for (int i = 0; i < count; i++)
                    {
                        string text = await options.Nth(i).TextContentAsync() ?? "";
                        if (NormalizeText(text) == fallbackNorm)
                        {
                            string val = await options.Nth(i).GetAttributeAsync("value") ?? "";
                            await selectLocator.SelectOptionAsync(new[] { val });
                            _logger($"  ✓ {fieldName} (PADRÃO): {text.Replace("\u00A0", " ").Trim()}");
                            return true;
                        }
                    }
                }

                _logger($"  ❌ {fieldName} falhou para '{searchText}'");
                return false;
            }
            catch
            {
                _logger($"  ❌ Erro de interface ao buscar {fieldName}");
                return false;
            }
        }

        private async Task CancelarFormularioAsync(IFrameLocator frame)
        {
            try
            {
                var btnCancelar = frame.Locator("a[href*=\"doAcao('can')\"] | img[src*='cancela.png']").First;
                if (await btnCancelar.IsVisibleAsync())
                {
                    await btnCancelar.ClickAsync();
                    await Task.Delay(1500);
                }
            }
            catch { }
        }
    }
}