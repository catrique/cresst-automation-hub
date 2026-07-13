using AutomationApp.Services.Betha;
using AutomationApp.Utils;

namespace AutomationApp.Controllers.Betha
{
    public class BethaAutomationController
    {
        public async Task SubmitAsosAsync()
        {
            Console.Clear();
            MessageConsole.Info("\n=== LANÇAMENTO DE ASOS VIA API (BETHA) ===");
            Console.Write("\nDigite ou cole o caminho completo da planilha Excel:\n> ");
            string pathSpreadsheet = Console.ReadLine()?.Trim() ?? "";
            pathSpreadsheet = pathSpreadsheet.Trim('"');

            if (string.IsNullOrWhiteSpace(pathSpreadsheet))
            {
                MessageConsole.Error("\n❌ Erro: O caminho da planilha não pode ser vazio.");
                return;
            }

            if (!File.Exists(pathSpreadsheet))
            {
                MessageConsole.Error($"\n❌ Erro: O arquivo não foi encontrado:\n[{pathSpreadsheet}]");
                return;
            }

            MessageConsole.Success($"\n✅ Arquivo detectado com sucesso: {Path.GetFileName(pathSpreadsheet)}\n");

            Action<string> logger = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

            var bethaService = new BethaAsoIntegrationService(logger);
            await bethaService.ProcessSpreadsheetAsosAsync(pathSpreadsheet);

            Console.ReadKey(true);
        }

        public async Task<string> ExportEmployeeNamesAsync()
        {
            Console.Clear();
            MessageConsole.Info("=== OBTER LISTAGEM DE SERVIDORES NO BETHA ===");
            Action<string> logger = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            var bethaService = new BethaReportEmployeeService(logger);
            string pathSpreadsheet = await bethaService.GenerateExcelEmployeeNamesBethaAsync();

            MessageConsole.Success($"\n✅ Arquivo detectado com sucesso: {Path.GetFileName(pathSpreadsheet)}\n");
            return pathSpreadsheet;
        }
    }
}