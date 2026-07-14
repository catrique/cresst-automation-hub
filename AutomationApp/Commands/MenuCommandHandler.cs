using AutomationApp.Controllers.Betha;
using AutomationApp.Controllers.Config;
using AutomationApp.Controllers.Soc;
using Spectre.Console;

namespace AutomationApp.Commands
{
    public class MenuCommandHandler
    {
        public async Task<int> HandleInteractiveMenuAsync()
        {
            while (true)
            {
                Console.Clear();
                AnsiConsole.Write(
                    new FigletText("CRESST AUTOMATION")
                        .Centered()
                        .Color(Color.Cyan1));

                AnsiConsole.Write(new Rule("[yellow]PAINEL DE AUTOMAÇÕES E INTEGRAÇÃO[/]").Centered());
                Console.WriteLine();

                var selectedOption = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Selecione a operação que deseja executar:")
                        .PageSize(12)
                        .MoreChoicesText("[grey](Mova com as setas ↑ e ↓, confirme com Enter)[/]")
                        .AddChoices(new[] {
                            "1. Baixar e Organizar ASOs (SOC)",
                            "2. Organizar ASOs (Diretório SOC)",
                            "3. Atualizar Credenciais do SOC",
                            "4. Atualizar Credenciais do Betha",
                            "5. Atualizar Configurações de Proxy",
                            "6. Atualizar Token de Autorização",
                            "7. Lançar ASOs via API (Betha)",
                            "8. Higienizar e Validar Funcionários (SOC)",
                            "9. Agendar Compromissos/Exames (SOC)",
                            "10. Corrigir Nomes de Funcionários no SOC",
                            "0. Sair do Sistema"
                        }));

                if (selectedOption.StartsWith("0"))
                {
                    AnsiConsole.MarkupLine("[yellow]Saindo do sistema... Até logo![/]");
                    break;
                }

                string optionKey = selectedOption.Split('.')[0].Trim();

                await ProcessOptionAsync(optionKey);

                AnsiConsole.MarkupLine("\nPressione [green]qualquer tecla[/] para voltar ao menu principal...");
                Console.ReadKey(true);
            }

            return 0;
        }

        private async Task ProcessOptionAsync(string option)
        {
            var configController = new ConfigAutomationController();
            var bethaController = new BethaAutomationController();
            var socController = new SocAutomationController();

            switch (option)
            {
                case "1":
                    await socController.DownloadAsosAsync();
                    break;

                case "2":
                    await socController.OrganizeAsosAsync();
                    break;

                case "3":
                    await configController.UpdateSocCredentialsAsync();
                    break;

                case "4":
                    await configController.UpdateBethaCredentialsAsync();
                    break;

                case "5":
                    await configController.UpdateProxySettingsAsync();
                    break;
                case "6":
                    string token =await bethaController.getAuthorizationToken();
                    await configController.UpdateBearerTokenSettingsAsync(token);
                    break;

                case "7":
                    await bethaController.SubmitAsosAsync();
                    break;
                case "8":
                    var socEmployeeController = new SocAutomationController();
                    await socEmployeeController.RegisterEmployeesAsync();
                    break;
                case "9":
                    var socAppointmentController = new SocAutomationController();
                    await socAppointmentController.ScheduleAppointmentsAsync(paginaAtiva: null);
                    break;
                case "10":
                    var socUpdateEmployeeNamesController = new SocAutomationController();
                    // var bethaExportEmployeeNamesController = new BethaAutomationController();
                    // string pathSpreadsheetBethaEmployeeNames = await bethaExportEmployeeNamesController.ExportEmployeeNamesAsync();
                    string pathSpreadsheetBethaEmployeeNames = @"C:\Users\42706671840\Downloads\cresst-workspace\relatorios\employees_09-07-2026_12-24.xlsx";
                    await socUpdateEmployeeNamesController.UpdateEmployeeNamesAsync(pathSpreadsheetBethaEmployeeNames);
                    break;

                default:
                    AnsiConsole.MarkupLine("[red]❌ Opção inválida selecionada![/]");
                    break;
            }
        }
    }
}