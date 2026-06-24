using AutomationApp.Services.Soc.Downloading;
using AutomationApp.Services.Soc.Organizing;
using Microsoft.Playwright;

namespace AutomationApp.Services.Soc;

public class SocAsoService : SocBaseService
{
    private readonly Action<string> _logger;
    private readonly IAsoOrganizerService _organizer;
    private readonly IFileDownloadStrategy _downloadStrategy;
    private readonly AsoFilePersister _filePersister;

    private const string FrameId = "#novosocFrame";

    public SocAsoService(Action<string> logger)
    {
        _logger = logger;
        _organizer = new AsoOrganizerService(logger);
        _downloadStrategy = new BlobNewTabDownloadStrategy();
        _filePersister = new AsoFilePersister();
    }

    public async Task DownloadAsosByPeriodAsync(string startDate, string endDate)
    {
        try
        {
            _logger("Initializing browser and authenticating into SOC...");
            await InitializeAndLoginAsync();

            _logger("Navigating directly to Screen 611 (SOCGED Filters)...");
            await NavigateToScreenCodeAsync("611");

            var frame = _page?.FrameLocator(FrameId);

            _logger("Selecionando o tipo de documento: 'ASO'...");
            await frame!.Locator("#codigoTipoGed").SelectOptionAsync(new[] { "7" });

            _logger($"Applying date filters: {startDate} up to {endDate}");

            await frame.Locator("#dataInicial").ClearAsync();
            await frame.Locator("#dataInicial").FillAsync(startDate);

            await frame.Locator("#dataFinal").ClearAsync();
            await frame.Locator("#dataFinal").FillAsync(endDate);

            _logger("Submitting filters...");
            await frame.Locator("img[name='botao-pesquisar-padrao-soc']").ClickAsync();

            await _page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(3000);

            string? pastaDestino = null;
            bool temProximaPagina = true;
            int paginaAtual = 1;

            while (temProximaPagina)
            {
                _logger($"Escaneando a página em busca de arquivos para download {paginaAtual}...");
                pastaDestino = await ProcessPageDownloadsAsync(frame);

                var btnProximo = frame.Locator("//div[@id='barraInferior']//a[@id='btn_proximo']");
                int btnCount = await btnProximo.CountAsync();

                if (btnCount > 0)
                {
                    var btnClass = await btnProximo.GetAttributeAsync("class") ?? "";

                    if (!btnClass.Contains("disabled"))
                    {
                        _logger("Navegando para a próxima página...");
                        await btnProximo.ScrollIntoViewIfNeededAsync();
                        await btnProximo.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        await Task.Delay(4000);
                        paginaAtual++;
                    }
                    else
                    {
                        _logger("No more pages left to process.");
                        temProximaPagina = false;
                    }
                }
                else
                {
                    _logger("No more pages left to process.");
                    temProximaPagina = false;
                }
            }

            if (pastaDestino != null)
            {
                _logger("🗂️ Iniciando organização dos ASOs baixados...");
                await _organizer.OrganizeAsync(pastaDestino);
            }
        }
        finally
        {
            _logger("Closing automation session...");
            await CloseAsync();
        }
    }

    private async Task<string> ProcessPageDownloadsAsync(IFrameLocator frame)
    {
        string rawStart = await frame.Locator("#dataInicial").GetAttributeAsync("value") ?? "";
        string rawEnd = await frame.Locator("#dataFinal").GetAttributeAsync("value") ?? "";

        string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", Program.Settings.Paths.AsosDownloads);

        string destinationFolder = _filePersister.BuildDestinationFolder(
            rawStart, rawEnd, baseFolder);

        var detailButtons = frame.Locator("//a[img[@id='det']]");
        int total = await detailButtons.CountAsync();

        _logger($"Found {total} entries on this page. Saving to: {Path.GetFileName(destinationFolder)}");

        for (int i = 0; i < total; i++)
        {
            await ProcessSingleEntryAsync(frame, detailButtons.Nth(i), destinationFolder, $"{i + 1:D2}/{total:D2}");
        }

        return destinationFolder;
    }

    private async Task ProcessSingleEntryAsync(
        IFrameLocator frame, ILocator button, string destinationFolder, string progress)
    {
        try
        {
            await DismissModalAsync();
            await button.ScrollIntoViewIfNeededAsync();

            _logger($"[{progress}] Opening details modal...");
            await button.ClickAsync();
            await Task.Delay(1500);

            var viewIcon = frame.Locator("//span[@class='icone-visualizar-arquivo icones']");

            if (await viewIcon.CountAsync() == 0)
            {
                _logger($"[{progress}] ⚠️ No view icon found, skipping.");
                return;
            }

            _logger($"[{progress}] Extracting PDF from new tab...");

            var (content, filename) = await _downloadStrategy.DownloadAsync(_page!, viewIcon.First);
            await _filePersister.SaveAsync(content, filename, destinationFolder);

            _logger($"[{progress}] ✅ Saved: {filename}");
        }
        catch (Exception ex)
        {
            _logger($"[{progress}] ❌ Error: {ex.Message}");
        }
        finally
        {
            await DismissModalAsync();
            await CloseExtraTabsAsync();
            await Task.Delay(1000);
        }
    }

    private async Task DismissModalAsync()
    {
        try { await _page!.EvaluateAsync("$('#arquivosModal').modal('hide');"); } catch { }
    }

    private async Task CloseExtraTabsAsync()
    {
        if (_context is null) return;
        for (int p = _context.Pages.Count - 1; p > 0; p--)
            try { await _context.Pages[p].CloseAsync(); } catch { }
    }
}