using AutomationApp.Controllers.Config;
using AutomationApp.Controllers.Soc;

namespace AutomationApp.Commands
{
    public class MenuCommandHandler
    {
        public async Task<int> HandleInteractiveMenuAsync()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==================================================");
                Console.WriteLine("            CRESST AUTOMATION CLI                 ");
                Console.WriteLine("==================================================");
                Console.ResetColor();
                Console.WriteLine("1. Baixar e Organizar ASOs (SOC)");
                Console.WriteLine("2. Atualizar Credenciais do SOC");
                Console.WriteLine("3. Atualizar Credenciais do Betha");
                Console.WriteLine("4. Atualizar Configurações de Proxy");
                Console.WriteLine("5. Atualizar Token");
                Console.WriteLine("6. Lançar ASOs via API (Betha)");
                Console.WriteLine("0. Sair");
                Console.Write("\nEscolha uma opção: ");

                string opcao = Console.ReadLine()?.Trim() ?? "";

                if (opcao == "0")
                {
                    Console.WriteLine("Saindo do sistema...");
                    break;
                }

                await ProcessarOpcaoAsync(opcao);
            }

            return 0;
        }

        private async Task ProcessarOpcaoAsync(string opcao)
        {
            var configController = new ConfigAutomationController();

            switch (opcao)
            {
                case "1":
                    var socController = new SocAutomationController();
                    await socController.DownloadAsosAsync();
                    break;

                case "2":
                    await configController.UpdateSocCredentialsAsync();
                    break;

                case "3":
                    await configController.UpdateBethaCredentialsAsync();
                    break;

                case "4":
                    await configController.UpdateProxySettingsAsync();
                    break;
                case "5":
                    await configController.UpdateBearerTokenSettingsAsync();
                    break;
                    
                case "6":
                    var bethaController = new AutomationApp.Controllers.Betha.BethaAutomationController();
                    await bethaController.SubmitAsosAsync();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ Opção inválida!");
                    Console.ResetColor();
                    break;
            }

            Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
        }
    }
}