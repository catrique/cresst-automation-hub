using System.Text.Json;

namespace AutomationApp.Services.Betha
{
    public class BethaEmployeeCheckerService
    {
        private readonly IBethaApiService? _apiMiddleware;
        private readonly Action<string> _logger;

        public BethaEmployeeCheckerService(IBethaApiService? apiMiddleware, Action<string> logger)
        {
            _apiMiddleware = apiMiddleware;
            _logger = logger ?? (msg => { });
        }

        public async Task<bool> VerificarMatriculaExisteAsync(string matricula)
        {
            if (_apiMiddleware == null) 
                return false;

            try
            {
                if (Program.Settings?.Betha?.Api?.Endpoints == null || 
                    !Program.Settings.Betha.Api.Endpoints.TryGetValue("RegistrationList", out string? endpoint) ||
                    string.IsNullOrWhiteSpace(endpoint))
                    endpoint = "rh/api/matricula/listagem-matricula";

                string matLimpa = matricula.Trim();
                string numeroBase = matLimpa;
                string contrato = "1";

                if (matLimpa.Contains('/'))
                {
                    var partes = matLimpa.Split('/');
                    numeroBase = partes[0].Trim();
                    if (partes.Length > 1)
                        contrato = partes[1].Trim();
                }

                string filterExpression = $"(pessoa.nome elike \"%25{matLimpa}%25\") or (codigo.numero = \"{numeroBase}\" and codigo.contrato = \"{contrato}\") or pessoa.cpf = \"{numeroBase}{contrato}\" or pessoa.identidade = \"{numeroBase}{contrato}\" or pessoa.pis = \"{numeroBase}{contrato}\"";
                string queryString = $"filter={Uri.EscapeDataString(filterExpression)}&filtroSituacao=TODOS&limit=20&offset=0&selecaoAvancada=&sort=";
                string? jsonResponse = await _apiMiddleware.SendGetRequestAsync(endpoint, queryString);

                if (string.IsNullOrEmpty(jsonResponse)) 
                    return false;

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("total", out JsonElement totalElement))
                {
                    int totalRegistros = totalElement.GetInt32();
                    if (totalRegistros > 0)
                        return true; 
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger($"    ⚠️ [BethaChecker] Erro ao consultar matrícula [{matricula}]: {ex.Message}. Ignorando.");
                return false;
            }
        }
    }
}