using AutomationApp.Services.Soc;

namespace AutomationApp.Controllers.Soc
{
    public class SocAutomationController
    {
        private readonly Action<string> _logger;

        public SocAutomationController()
        {
            _logger = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public async Task<int> DownloadAsosAsync()
        {
            Console.Write("\nDigite a data de INÍCIO (ex: 10/06/2026): ");
            string dataInicio = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Digite a data de FIM    (ex: 16/06/2026): ");
            string dataFim = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(dataInicio) || string.IsNullOrWhiteSpace(dataFim))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Erro: As datas não podem ser vazias.");
                Console.ResetColor();
                Console.WriteLine("\nUso correto: AutomationApp.exe <data_inicio> <data_fim>");
                Console.WriteLine("Exemplo:    AutomationApp.exe 01/06/2026 05/06/2026\n");
                return 1;
            }

            string formato = "dd/MM/yyyy";
            var cultura = System.Globalization.CultureInfo.InvariantCulture;

            if (!DateTime.TryParseExact(dataInicio, formato, cultura, System.Globalization.DateTimeStyles.None, out _) ||
    !DateTime.TryParseExact(dataFim, formato, cultura, System.Globalization.DateTimeStyles.None, out _))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Erro: Uma ou ambas as datas estão em um formato inválido.");
                Console.WriteLine($"As datas devem seguir estritamente o padrão {formato}.");
                Console.ResetColor();
                return 1;
            }

            try
            {
                _logger($"Iniciando o processo de automação de ASOs...");
                var socService = new SocAsoService(_logger);
                await socService.DownloadAsosByPeriodAsync(dataInicio, dataFim);

                _logger("Pipeline finalizado com sucesso.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                _logger($"Erro durante a execução da automação: {ex.Message}");
                Console.ResetColor();
                return 2;
            }
        }
    }
}