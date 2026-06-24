using AutomationApp.Services.Betha;

namespace AutomationApp.Controllers.Betha
{
    public class BethaAutomationController
    {
        public async Task SubmitAsosAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== LANÇAMENTO DE ASOS VIA API (BETHA) ===");
            Console.ResetColor();

            Console.Write("\nDigite ou cole o caminho completo da planilha Excel:\n> ");
            string pathSpreadsheet = Console.ReadLine()?.Trim() ?? "";

            pathSpreadsheet = pathSpreadsheet.Trim('"');

            if (string.IsNullOrWhiteSpace(pathSpreadsheet))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Erro: O caminho da planilha não pode ser vazio.");
                Console.ResetColor();
                return;
            }

            if (!File.Exists(pathSpreadsheet))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Erro: O arquivo não foi encontrado:\n[{pathSpreadsheet}]");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ Arquivo detectado com sucesso: {Path.GetFileName(pathSpreadsheet)}\n");
            Console.ResetColor();

            Action<string> logger = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

            var bethaService = new BethaAsoIntegrationService(logger);
            
            await bethaService.ProcessSpreadsheetAsosAsync(pathSpreadsheet);

            Console.WriteLine("\n[Fim do Bloco de Execução do Controlador]");
        }
    }
}