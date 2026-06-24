using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc.Organizing;

public class AsoOrganizerService : IAsoOrganizerService
{
    private readonly Action<string>   _logger;
    private readonly AsoPdfReader     _reader    = new();
    private readonly AsoFileOrganizer _organizer = new();
    private readonly AsoExcelReporter _reporter  = new();
    private readonly AsoReturnListWriter _retWriter = new();

    public AsoOrganizerService(Action<string> logger) => _logger = logger;

    public Task OrganizeAsync(string folderPath)
    {
        var pdfs = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
        _logger($"📄 {pdfs.Length} PDFs encontrados para organizar.");

        var results = new List<AsoData>();
        int ok = 0, err = 0;

        for (int i = 0; i < pdfs.Length; i++)
        {
            var progress = $"[{i + 1:D2}/{pdfs.Length:D2}]";
            var data     = _reader.Extract(pdfs[i]);

            if (!data.LeituraOk)
            {
                _logger($"{progress} ❌ Ilegível: {Path.GetFileName(pdfs[i])}");
                err++;
            }

            data.TipoExame = _organizer.ResolveExamType(data.TipoExame);

            var newPath  = _organizer.MoveToSubfolder(data, folderPath);
            data.PdfPath = newPath;

            if (data.LeituraOk)
            {
                _logger($"{progress} ✅ {Path.GetFileName(newPath)}");
                ok++;
            }

            results.Add(data);
        }

        _logger("📊 Gerando Relatorio_Completo.xlsx...");
        _reporter.Generate(results, folderPath);

        _logger("📝 Gerando Lista_Retorno_Trabalho.txt...");
        _retWriter.Generate(results, folderPath);

        _logger($"{'=',-45}");
        _logger($"RESUMO: {ok} organizados | {err} com erro");
        _logger($"{'=',-45}");

        return Task.CompletedTask;
    }
}