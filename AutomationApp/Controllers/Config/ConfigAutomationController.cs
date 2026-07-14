using System.Text.Json;
using AutomationApp.Services.Utils;

namespace AutomationApp.Controllers.Config
{
    public class ConfigAutomationController
    {
        public async Task<int> UpdateSocCredentialsAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== ATUALIZAR CREDENCIAIS DO SOC ===");
            Console.ResetColor();

            Console.Write("Digite o Login do SOC: ");
            string login = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Digite a Senha do SOC: ");
            string password = ReadPassword();

            Console.Write("Digite a Senha Virtual do SOC: ");
            string virtualPassword = ReadPassword();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(virtualPassword))
            {
                ShowError("Todos os campos do SOC são obrigatórios.");
                return 1;
            }

            Program.Settings.Soc.Credentials.Login = DataProtectionService.Encrypt(login);
            Program.Settings.Soc.Credentials.Password = DataProtectionService.Encrypt(password);
            Program.Settings.Soc.Credentials.VirtualPassword = DataProtectionService.Encrypt(virtualPassword);

            SaveSettingsToDisk();

            ShowSuccess("Credenciais do SOC criptografadas e salvas na memória com sucesso!");
            return 0;
        }

        public async Task<int> UpdateBethaCredentialsAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== ATUALIZAR CREDENCIAIS DO BETHA ===");
            Console.ResetColor();

            Console.Write("Digite o Login do Betha: ");
            string login = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Digite a Senha do Betha: ");
            string password = ReadPassword();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Todos os campos do Betha são obrigatórios.");
                return 1;
            }

            Program.Settings.Betha.Credentials.Login = DataProtectionService.Encrypt(login);
            Program.Settings.Betha.Credentials.Password = DataProtectionService.Encrypt(password);
            SaveSettingsToDisk();
            ShowSuccess("Credenciais do Betha criptografadas e salvas na memória com sucesso!");
            return 0;
        }

        public async Task<int> UpdateProxySettingsAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== ATUALIZAR CONFIGURAÇÕES DE PROXY ===");
            Console.ResetColor();

            Console.Write("Digite o Usuário do Proxy (deixe vazio se não houver): ");
            string username = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Login é obrigatório.");
                return 1;
            }

            Console.Write("Digite a Senha do Proxy (deixe vazio se não houver): ");
            string password = ReadPassword();

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Senha é obrigatório.");
                return 1;
            }


            Program.Settings.Proxy.Username = string.IsNullOrWhiteSpace(username) ? string.Empty : DataProtectionService.Encrypt(username);
            Program.Settings.Proxy.Password = string.IsNullOrWhiteSpace(password) ? string.Empty : DataProtectionService.Encrypt(password);
            SaveSettingsToDisk();
            ShowSuccess("Configurações de Proxy salvas na memória com sucesso!");
            return 0;
        }

        public async Task<int> UpdateBearerTokenSettingsAsync(string token)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== ATUALIZANDO BEARER TOKEN ===");
            Console.ResetColor();

            if (string.IsNullOrWhiteSpace(token))
            {
                ShowError("Token é obrigatório");
                return 1;
            }

            Program.Settings.Betha.Api.Authorization = string.IsNullOrWhiteSpace(token) ? string.Empty : "Bearer " + token;
            SaveSettingsToDisk();
            ShowSuccess("Token salvo na memória com sucesso!");
            return 0;
        }


        private string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        private void SaveSettingsToDisk()
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
                string fileName = $"appsettings.{environment}.json";
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string updatedJson = JsonSerializer.Serialize(Program.Settings, options);

                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠️ Alerta: Não foi possível persistir as configurações no disco: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Erro: {message}");
            Console.ResetColor();
        }

        private void ShowSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ {message}");
            Console.ResetColor();
        }
    }
}