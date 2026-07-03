using System.Globalization;
using System.Text.Json;

namespace AutomationApp.Services.Betha
{
    public class BethaAsoCheckerService
    {
        private readonly IBethaApiService? _apiMiddleware;
        private readonly Action<string> _logger;
        private readonly AsoBusinessRulesService _rulesService = new();

        public BethaAsoCheckerService(IBethaApiService? apiMiddleware, Action<string> logger)
        {
            _apiMiddleware = apiMiddleware;
            _logger = logger ?? (msg => { });
        }

        public async Task<string> VerificarSeAsoExisteAsync(string matricula, string dataExame, string tipoExame)
        {
            if (_apiMiddleware == null) return "ERRO_MIDDLEWARE";
            string endpointAso = Program.Settings.Betha?.Api?.Endpoints != null &&
                                 Program.Settings.Betha.Api.Endpoints.TryGetValue("Aso", out string? endp)
                                 ? endp : "aso";

            if (string.IsNullOrWhiteSpace(matricula)) return "MATRICULA_INVALIDA";
            string numeroMatricula = "";
            string numeroContrato = "";

            if (matricula.Contains('/'))
            {
                var partes = matricula.Split('/');
                numeroMatricula = new string(partes[0].Where(char.IsDigit).ToArray());
                if (partes.Length > 1)
                    numeroContrato = new string(partes[1].Where(char.IsDigit).ToArray());
            }
            else
                numeroMatricula = new string(matricula.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(numeroMatricula)) return "MATRICULA_INVALIDA";

            string tipoExamePlanilha;
            try
            {
                tipoExamePlanilha = _rulesService.NormalizeExamType(tipoExame);
            }
            catch (Exception)
            {
                return "TIPO_EXAME_INVALIDO";
            }

            if (!DateTime.TryParseExact(dataExame.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataPlanilhaParsed))
                return "DATA_PLANILHA_INVALIDA";

            try
            {
                string filtroOData;
                if (!string.IsNullOrEmpty(numeroContrato))
                    filtroOData = $"((matricula.codigoMatricula.numero = {numeroMatricula} and matricula.codigoMatricula.contrato = {numeroContrato})) and (conclusaoAso in (\"APTO\",\"APTO_COM_RESTRICOES\",\"INAPTO\",\"INCONCLUSIVO\"))";
                else
                    filtroOData = $"((matricula.codigoMatricula.numero = {numeroMatricula})) and (conclusaoAso in (\"APTO\",\"APTO_COM_RESTRICOES\",\"INAPTO\",\"INCONCLUSIVO\"))";

                string query = $"filter={Uri.EscapeDataString(filtroOData)}&limit=20&offset=0&situacao=VALIDO&situacao=PROXIMO_DO_VENCIMENTO&situacao=VENCIDO";

                string? jsonResposta = await _apiMiddleware.SendGetRequestAsync(endpointAso, query);

                if (string.IsNullOrEmpty(jsonResposta)) return "NAO_ENCONTRADO";

                using JsonDocument doc = JsonDocument.Parse(jsonResposta);

                if (doc.RootElement.TryGetProperty("content", out JsonElement contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in contentArray.EnumerateArray())
                    {
                        string apiTipoExame = item.TryGetProperty("tipoExameAso", out JsonElement t) ? t.GetString() ?? "" : "";
                        string apiDataStr = item.TryGetProperty("data", out JsonElement d) ? d.GetString() ?? "" : "";

                        if (DateTime.TryParseExact(apiDataStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataApiParsed))
                        {
                            if (string.Equals(apiTipoExame.Trim(), tipoExamePlanilha.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                dataApiParsed.Date == dataPlanilhaParsed.Date)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                _logger($"   🔍 [Checker] ASO já cadastrado na Betha para a matrícula {numeroMatricula} em {dataExame} ({apiTipoExame}).");
                                Console.ResetColor();

                                return "JA_CADASTRADO";
                            }
                        }
                    }
                }

                return "NAO_ENCONTRADO";
            }
            catch (Exception ex)
            {
                _logger($"⚠️ Erro interno no CheckerService: {ex.Message}");
                return "INCONCLUSIVO";
            }
        }
    }
}