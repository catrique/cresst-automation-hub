using AutomationApp.Services.Soc;
using AutomationApp.Utils;
using AutomationApp.Models.Soc;
using Microsoft.Playwright;
using AutomationApp.Services.Soc.Organizing;

namespace AutomationApp.Controllers.Soc
{
    public class SocAutomationController
    {
        private readonly Action<string> _logger;

        public SocAutomationController()
        {
            _logger = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public async Task RegisterEmployeesAsync()
        {
            Console.Clear();
            MessageConsole.Info("=== HIGIENIZAÇÃO E CADASTRO DE FUNCIONÁRIOS (SOC) ===");

            MessageConsole.Info("\nDigite ou cole o caminho completo da planilha Excel:\n> ");
            string pathSpreadsheet = Console.ReadLine()?.Trim() ?? "";
            pathSpreadsheet = pathSpreadsheet.Trim('\"');

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

            MessageConsole.Info("--- Qual é o Setor Base/Padrão?\n> 1 - EDUCAÇÃO\n> 2 - SAÚDE\n> 3 - ADMINISTRATIVO\n> ");
            int setorBase = int.Parse(Console.ReadLine()?.Trim() ?? "1");


            _logger("Iniciando processamento da planilha de funcionários...");
            var integrationService = new SocEmployeeIntegrationService(_logger);
            var employees = await integrationService.ProcessEmployeeSpreadsheetAsync(pathSpreadsheet);

            _logger("Iniciando automação web de cadastro...");
            var webService = new SocEmployeeWebAutomationService(_logger);

            await webService.ExecuteWebAutomationAsync(employees, setorBase);

            MessageConsole.Success("\n✅ Processo de Cadastro de Funcionários Finalizado!");

            MessageConsole.Info("\n👉 Deseja prosseguir para os Agendamentos de Exames (Tela 236) usando esta mesma janela do navegador? (S/N): ");
            string resposta = Console.ReadLine()?.Trim().ToUpper() ?? "N";

            if (resposta == "S" || resposta == "SIM")
            {
                IPage? paginaAtiva = webService.GetPage();

                await ScheduleAppointmentsAsync(paginaAtiva, pathSpreadsheet);
            }
            else
            {
                await webService.CloseAsync();
            }
        }

        public async Task ScheduleAppointmentsAsync(IPage? paginaAtiva = null, string caminhoPlanilhaPredefinido = "")
        {
            if (paginaAtiva == null)
                Console.Clear();

            MessageConsole.Info("\n=== AGENDAMENTO DE EXAMES/COMPROMISSOS (SOC) ===");

            string pathSpreadsheet = caminhoPlanilhaPredefinido;

            if (string.IsNullOrWhiteSpace(pathSpreadsheet))
            {
                MessageConsole.Info("\nDigite ou cole o caminho completo da planilha Excel de Agendamentos:\n> ");
                pathSpreadsheet = Console.ReadLine()?.Trim() ?? "";
                pathSpreadsheet = pathSpreadsheet.Trim('\"');
            }

            if (string.IsNullOrWhiteSpace(pathSpreadsheet) || !File.Exists(pathSpreadsheet))
            {
                MessageConsole.Error("\n❌ Erro: Arquivo de agendamentos não encontrado ou caminho inválido.");
                return;
            }

            Console.WriteLine("\nQual é o Tipo de Compromisso?");
            Console.WriteLine("1️⃣ - Admissional");
            Console.WriteLine("2️⃣ - Periódico");
            Console.WriteLine("3️⃣ - Retorno ao Trabalho");
            Console.WriteLine("4️⃣ - Mudança de Riscos Ocupacionais");
            Console.WriteLine("5️⃣ - Demissional");
            Console.WriteLine("6️⃣ - Monitoração Pontual");
            MessageConsole.Info("\n Escolha uma opção (1-6): ");

            string tipoCompromisso = Console.ReadLine()?.Trim() ?? "1";
            var appointmentIntegration = new SocAppointmentIntegrationService(_logger);
            var appointments = appointmentIntegration.ProcessAppointmentSpreadsheet(pathSpreadsheet);
            var appointmentWebService = new SocAppointmentWebAutomationService(_logger);

            await appointmentWebService.ExecuteAppointmentAutomationAsync(appointments, tipoCompromisso, paginaAtiva);

            MessageConsole.Success("\n✅ Pipeline de Agendamento de Exames Concluído!");
        }

        public async Task<int> DownloadAsosAsync()
        {
            string[] args = Environment.GetCommandLineArgs();
            string? dataInicio = args.Length > 1 ? args[1] : null;
            string? dataFim = args.Length > 2 ? args[2] : null;

            if (string.IsNullOrWhiteSpace(dataInicio) || string.IsNullOrWhiteSpace(dataFim))
            {
                MessageConsole.Info("Digite a data de início (dd/MM/yyyy): ");
                dataInicio = Console.ReadLine();

                MessageConsole.Info("Digite a data de fim (dd/MM/yyyy): ");
                dataFim = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(dataInicio) || string.IsNullOrWhiteSpace(dataFim))
            {
                MessageConsole.Error("\n❌ Erro: As datas não podem ser vazias.");
                return 1;
            }

            string formato = "dd/MM/yyyy";
            var cultura = System.Globalization.CultureInfo.InvariantCulture;

            if (!DateTime.TryParseExact(dataInicio, formato, cultura, System.Globalization.DateTimeStyles.None, out _) ||
                !DateTime.TryParseExact(dataFim, formato, cultura, System.Globalization.DateTimeStyles.None, out _))
            {
                MessageConsole.Error($"\n❌ Erro: Uma ou ambas as datas estão em um formato inválido.\nAs datas devem seguir estritamente o padrão {formato}.");
                return 1;
            }

            try
            {
                _logger("Iniciando o processo de automação de ASOs...");
                var socService = new SocAsoService(_logger);
                await socService.DownloadAsosByPeriodAsync(dataInicio, dataFim);

                _logger("Pipeline finalizado com sucesso.");
                return 0;
            }
            catch (Exception ex)
            {
                MessageConsole.Error($"Erro durante a execução da automação: {ex.Message}");
                return 1;
            }
        }

        public async Task<bool> UpdateEmployeeNamesAsync(string pathSpreadsheetBethaEmployeeNames)
        {
            Console.Clear();
            MessageConsole.Info("=== CORREÇÃO DO NOME DE CADASTRO DOS SERVIDORES(SOC) ===");

            pathSpreadsheetBethaEmployeeNames.Trim('\"');

            if (string.IsNullOrWhiteSpace(pathSpreadsheetBethaEmployeeNames))
            {
                MessageConsole.Error("\n❌ Erro: O caminho da planilha não pode ser vazio.");
                return false;
            }

            if (!File.Exists(pathSpreadsheetBethaEmployeeNames))
            {
                MessageConsole.Error($"\n❌ Erro: O arquivo não foi encontrado:\n[{pathSpreadsheetBethaEmployeeNames}]");
                return false;
            }

            MessageConsole.Success($"\n✅ Arquivo detectado com sucesso: {Path.GetFileName(pathSpreadsheetBethaEmployeeNames)}\n");

            try
            {
                _logger("Iniciando o processo de correção de nomes no SOC");
                var socService = new SocUpdateEmployeeNameByReportBethaService(_logger, pathSpreadsheetBethaEmployeeNames);
                await socService.ProcessSpreadsheetBethaAsync();

                _logger("Pipeline finalizado com sucesso.");
                return false;
            }
            catch (Exception ex)
            {
                MessageConsole.Error($"Erro durante a execução da automação: {ex.Message}");
                return true;
            }
        }

        public async Task OrganizeAsosAsync()
        {
            Console.Clear();
            MessageConsole.Info("=== ORGANIZAÇÃO DE ASOs (SOC) ===");

            MessageConsole.Info("\nDigite ou cole o caminho da pasta:\n> ");
            string dirPath = Console.ReadLine()?.Trim() ?? "";
            dirPath = dirPath.Trim('\"');

            if (string.IsNullOrWhiteSpace(dirPath))
            {
                MessageConsole.Error("\n❌ Erro: O caminho da pasta não pode ser vazio.");
                return;
            }

            if (!Directory.Exists(dirPath))
            {
                MessageConsole.Error($"\n❌ Erro: A pasta não foi encontrada:\n[{dirPath}]");
                return;
            }

            MessageConsole.Success($"\n✅ Pasta detectada com sucesso: {Path.GetFileName(dirPath)}\n");


            try
            {
                _logger("Iniciando o processo de organização de ASOs...");
                var asoOrganizerService = new AsoOrganizerService(_logger);
                await asoOrganizerService.OrganizeAsync(dirPath);

                _logger("Pipeline de organização de ASOs finalizado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageConsole.Error($"Erro durante a execução da automação: {ex.Message}");
            }
        }
    }
}