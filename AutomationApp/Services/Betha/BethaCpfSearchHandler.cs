using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AutomationApp.Services.Betha
{
    public class BethaCpfSearchHandler
    {
        public async Task<string?> PromptAndGetEmployeeIdAsync()
        {
            Console.Write("Digite o CPF do funcionário para buscar o ID real: ");
            string cpfInput = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(cpfInput))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠ Erro: O CPF não pode ser vazio.");
                Console.ResetColor();
                return null;
            }

            string cpfLimpo = cpfInput.Replace(".", "").Replace("-", "").Trim();

            if (cpfLimpo.Length != 11 || !long.TryParse(cpfLimpo, out _))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ Erro: O CPF [{cpfInput}] é inválido. Deve conter 11 dígitos numéricos.");
                Console.ResetColor();
                return null;
            }

            if (Program.Settings.Betha?.Api?.Endpoints == null ||
                !Program.Settings.Betha.Api.Endpoints.TryGetValue("RegistrationList", out string? endpoint))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Erro de Configuração: Rota 'RegistrationList' não mapeada no JSON.");
                Console.ResetColor();
                return null;
            }

            string filtroStr = $"pessoa.cpf = \"{cpfLimpo}\"";
            string queryString = $"filter={Uri.EscapeDataString(filtroStr)}&filtroSituacao=TODOS&limit=20&offset=0";

            Console.WriteLine("\nConectando à API Betha através do Middleware... Aguarde.");

            try
            {
                var bethaApiMiddleware = Program.ServiceProvider?.GetService<IBethaApiService>();

                if (bethaApiMiddleware == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ Erro: O Middleware BethaApiService não foi inicializado.");
                    Console.ResetColor();
                    return null;
                }

                string? jsonResult = await bethaApiMiddleware.SendGetRequestAsync(endpoint, queryString);

                if (string.IsNullOrEmpty(jsonResult)) return null;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n================ RESPOSTA JSON BRUTA ================");
                Console.WriteLine(jsonResult);
                Console.WriteLine("=====================================================\n");
                Console.ResetColor();

                using JsonDocument doc = JsonDocument.Parse(jsonResult);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("content", out JsonElement contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array &&
                    contentArray.GetArrayLength() > 0)
                    if (contentArray[0].TryGetProperty("id", out JsonElement idProp))
                        return idProp.ToString();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠ Nenhum funcionário localizado para o CPF {cpfInput}.");
                Console.ResetColor();
                return null;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Erro na operação: {ex.Message}");
                Console.ResetColor();
                return null;
            }
        }
    }
}