using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.Playwright;
using AutomationApp.Utils;

namespace AutomationApp.Services.Soc
{
  
    public class SocUpdateEmployeeNameByReportBethaService : SocBaseService
    {
        private const string FrameId = "#novosocFrame";
        private const string SearchResultRowSelector = "table.resultados tr.cor1, table.resultados tr.cor2";
        private const string SearchResultCodeLinkSelector = "td.codigo a.linkln";
        private const string UpdatedColumnHeader = "Atualizado";
        private readonly Action<string> _logger;
        private readonly string _spreadsheetPath;
        private readonly record struct EmployeeSearchResult(string Token, string DisplayCode, string CurrentName);
        private enum EmployeeUpdateOutcome
        {
            Updated,
            AlreadyUpToDate,
            NotFound,
            Failed
        }

        public SocUpdateEmployeeNameByReportBethaService(Action<string> logger, string spreadsheetPath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(spreadsheetPath))
                throw new ArgumentException("Caminho da planilha não informado.", nameof(spreadsheetPath));

            _spreadsheetPath = spreadsheetPath;
        }

        public async Task ProcessSpreadsheetBethaAsync()
        {
            using var workbook = new XLWorkbook(_spreadsheetPath);
            var worksheet = workbook.Worksheets.First();

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
            {
                _logger("⚠️ Planilha sem dados para processar.");
                return;
            }

            int updatedColumnIndex = GetOrCreateUpdatedColumnIndex(worksheet);

            try
            {
                await InitializeAndLoginAsync();
                await NavigateToScreenCodeAsync("232"); 

                var frame = _page?.FrameLocator(FrameId);
                if (frame == null)
                    throw new InvalidOperationException($"Frame '{FrameId}' não localizado.");

                for (int row = 2; row <= lastRow; row++)
                {
                    await ProcessRowAsync(worksheet, row, updatedColumnIndex, frame);
                }
            }
            finally
            {
                MessageConsole.Success("\n--- Atualização de nomes concluída! ---\n");
            }
        }

        private async Task ProcessRowAsync(IXLWorksheet worksheet, int row, int updatedColumnIndex, IFrameLocator frame)
        {
            string name = worksheet.Cell(row, 1).GetValue<string>()?.Trim();
            string cpf = worksheet.Cell(row, 2).GetValue<string>()?.Trim();
            string existingStatus = worksheet.Cell(row, updatedColumnIndex).GetValue<string>()?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(cpf))
            {
                _logger($"⚠️ Linha {row} ignorada: nome ou CPF em branco.");
                return;
            }

            if (existingStatus is "Sim" or "Inexistente")
            {
                _logger($"⏭️ Linha {row} ({name}) já processada anteriormente ({existingStatus}). Pulando.");
                return;
            }

            _logger($"\n👤 [INICIANDO] {name}");

            var outcome = await ProcessEmployeeAsync(frame, name, cpf);

            MarkRowStatus(worksheet, row, updatedColumnIndex, outcome);
            SaveWorkbookCheckpoint(worksheet);
        }

        private async Task<EmployeeUpdateOutcome> ProcessEmployeeAsync(IFrameLocator frame, string newName, string cpf)
        {
            string currentEmployeeCode = null;

            try
            {
                var searchResults = await SearchEmployeesAsync(frame, radioValue: "3", searchValue: cpf);

                if (searchResults.Count == 0)
                {
                    _logger($"  ❌ CPF {cpf} não encontrado no SOC.");
                    return EmployeeUpdateOutcome.NotFound;
                }

                var pendingResults = FilterResultsThatNeedUpdate(searchResults, newName);

                if (pendingResults.Count == 0)
                {
                    _logger($"  ⏭️ {newName}: nome já está correto em todos os {searchResults.Count} registro(s). Pulando.");
                    return EmployeeUpdateOutcome.AlreadyUpToDate;
                }

                _logger($"  🔎 {pendingResults.Count} de {searchResults.Count} registro(s) precisam de atualização.");

                int updatedCount = 0;

                for (int i = 0; i < pendingResults.Count; i++)
                {
                    currentEmployeeCode = pendingResults[i].DisplayCode;
                  
                    if (i == 0)
                    {
                        _logger($"  📂 Abrindo registro {currentEmployeeCode}...");
                        await OpenSearchResultAsync(frame, pendingResults[0].Token);
                    }
                    else
                    {
                        _logger($"  🔁 Buscando próximo registro pendente (código {currentEmployeeCode})...");
                        await SearchEmployeesAsync(frame, radioValue: "1", searchValue: currentEmployeeCode);
                        await OpenSearchResultAsync(frame, employeeToken: null);
                    }

                    if (await VerifyRecordCpfAsync(frame, cpf))
                    {
                        await EditAndSaveNameAsync(frame, newName);
                        updatedCount++;
                    }

                    await ReturnToSearchScreenAsync(frame);
                }

                if (updatedCount == 0)
                {
                    _logger($"  ❌ Nenhum dos {pendingResults.Count} registro(s) teve o CPF confirmado. Nada foi alterado.");
                    return EmployeeUpdateOutcome.Failed;
                }

                _logger($"  ✅ {newName} atualizado com sucesso ({updatedCount} de {pendingResults.Count} registro(s) pendente(s)).");
                return EmployeeUpdateOutcome.Updated;
            }
            catch (Exception ex)
            {
                string codeInfo = currentEmployeeCode == null ? "" : $" (código {currentEmployeeCode})";
                _logger($"  ❌ Exceção ao processar {newName}{codeInfo}: {ex.Message}");
                await TryCancelFormAsync(frame);
                return EmployeeUpdateOutcome.Failed;
            }
        }

        private static List<EmployeeSearchResult> FilterResultsThatNeedUpdate(
            List<EmployeeSearchResult> searchResults, string newName)
        {
            string expectedName = CleanDisplayText(newName);

            return searchResults
                .Where(result => CleanDisplayText(result.CurrentName) != expectedName)
                .ToList();
        }

        private static string CleanDisplayText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string withoutNbsp = text.Replace("\u00A0", " ").Replace("&nbsp;", " ");
            return Regex.Replace(withoutNbsp, @"\s+", " ").Trim();
        }

        private async Task<List<EmployeeSearchResult>> SearchEmployeesAsync(IFrameLocator frame, string radioValue, string searchValue)
        {
            await ClickViaJavaScriptAsync(frame.Locator($"input[name='codigoPesquisaFuncionario'][value='{radioValue}']"));

            var searchInput = frame.Locator("input[name='nomeSeach']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync(searchValue);
            await searchInput.PressAsync("Enter");

            var rows = frame.Locator(SearchResultRowSelector);
            try
            {
                await rows.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 8000 });
            }
            catch (System.TimeoutException)
            {
            }

            int rowCount = await rows.CountAsync();
            _logger($"  🔍 Busca por '{searchValue}': {rowCount} resultado(s) encontrado(s).");

            var results = new List<EmployeeSearchResult>();
            for (int i = 0; i < rowCount; i++)
            {
                var row = rows.Nth(i);
                var codeLink = row.Locator(SearchResultCodeLinkSelector);

                string href = await codeLink.GetAttributeAsync("href") ?? "";
                var match = Regex.Match(href, @"selbrowse\('([^']+)'\)");
                string token = match.Success ? match.Groups[1].Value : string.Empty;

                string displayCode = CleanDisplayText(await codeLink.TextContentAsync() ?? "");
                string currentName = (await row.Locator("div.nome-funcionario-registrado").TextContentAsync() ?? "").Trim();

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(displayCode))
                {
                    _logger($"  ⚠️ Linha de resultado sem código reconhecível (href: '{href}'). Ignorada.");
                    continue;
                }

                results.Add(new EmployeeSearchResult(token, displayCode, currentName));
            }

            return results;
        }

        private async Task OpenSearchResultAsync(IFrameLocator frame, string employeeToken)
        {
            var link = string.IsNullOrEmpty(employeeToken)
                ? frame.Locator($"{SearchResultRowSelector} {SearchResultCodeLinkSelector}").First
                : frame.Locator($"a[href*=\"selbrowse('{employeeToken}')\"]").First;

            await ClickViaJavaScriptAsync(link);
            await Task.Delay(1500);
        }

        private async Task<bool> VerifyRecordCpfAsync(IFrameLocator frame, string expectedCpf)
        {
            await WaitForLoadingOverlayToHideAsync(frame);

            var cpfSpan = frame.Locator("label[for='cpf'] + span");
            await cpfSpan.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 8000 });

            string actualCpf = NormalizeCpf(await cpfSpan.TextContentAsync() ?? "");
            string expectedCpfNormalized = NormalizeCpf(expectedCpf);

            if (actualCpf != expectedCpfNormalized)
            {
                _logger($"  ⚠️ CPF do registro aberto ('{actualCpf}') não bate com o CPF da planilha ('{expectedCpfNormalized}'). Pulando este registro sem alterar.");
                return false;
            }

            _logger($"  ✓ CPF conferido: {actualCpf}");
            return true;
        }

        private static string NormalizeCpf(string text) =>
            new string((text ?? string.Empty).Where(char.IsDigit).ToArray());

        private async Task EditAndSaveNameAsync(IFrameLocator frame, string newName)
        {
            _logger("  ✏️ Clicando em Alterar...");
            await ClickViaJavaScriptAsync(frame.Locator("a[href*=\"doAcao('alt')\"]"));
            await Task.Delay(1000);

            await WaitForLoadingOverlayToHideAsync(frame);

            var nameField = frame.Locator("#nomeFuncionario");
            await nameField.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
            await nameField.ClearAsync();
            await nameField.FillAsync(newName);
            _logger($"  ✓ Nome preenchido: {newName}");

            _logger("  💾 Clicando em Gravar...");
            await ClickViaJavaScriptAsync(frame.Locator("a[href*=\"doAcao('save')\"]"));
            await Task.Delay(2000);
        }

        private async Task ReturnToSearchScreenAsync(IFrameLocator frame)
        {
            _logger("  ↩️ Voltando para a tela de pesquisa...");
            await ClickViaJavaScriptAsync(frame.Locator("a[href*=\"doAcao('browse')\"]"));
            await Task.Delay(1000);
        }

        private async Task TryCancelFormAsync(IFrameLocator frame)
        {
            try
            {
                var cancelButton = frame.Locator("a[href*=\"doAcao('can')\"], img[src*='cancela.png']").First;
                await ClickViaJavaScriptAsync(cancelButton, maxAttempts: 1);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger($"  ⚠️ Não foi possível cancelar o formulário após o erro: {ex.Message}");
            }
        }

        private static async Task ClickViaJavaScriptAsync(ILocator locator, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });
                    await locator.EvaluateAsync("element => element.click()");
                    return;
                }
                catch when (attempt < maxAttempts)
                {
                    await Task.Delay(500);
                }
            }
        }

        private static async Task WaitForLoadingOverlayToHideAsync(IFrameLocator frame)
        {
            try
            {
                await frame.Locator("#divCarregando").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 15000 });
            }
            catch (System.TimeoutException)
            {
            }
        }

        private static int GetOrCreateUpdatedColumnIndex(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.Row(1);
            int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

            for (int col = 1; col <= lastColumn; col++)
            {
                if (string.Equals(headerRow.Cell(col).GetValue<string>()?.Trim(), UpdatedColumnHeader, StringComparison.OrdinalIgnoreCase))
                    return col;
            }

            int newColumnIndex = lastColumn + 1;
            headerRow.Cell(newColumnIndex).SetValue(UpdatedColumnHeader);
            return newColumnIndex;
        }

        private static void MarkRowStatus(IXLWorksheet worksheet, int row, int columnIndex, EmployeeUpdateOutcome outcome)
        {
            string status = outcome switch
            {
                EmployeeUpdateOutcome.Updated => "Sim",
                EmployeeUpdateOutcome.AlreadyUpToDate => "Sim",
                EmployeeUpdateOutcome.NotFound => "Inexistente",
                _ => "Não"
            };

            worksheet.Cell(row, columnIndex).SetValue(status);
        }

        private void SaveWorkbookCheckpoint(IXLWorksheet worksheet)
        {
            try
            {
                worksheet.Workbook.Save();
            }
            catch (Exception ex)
            {
                _logger($"⚠️ Não foi possível salvar o checkpoint na planilha: {ex.Message}");
            }
        }
    }
}