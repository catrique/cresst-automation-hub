using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using AutomationApp.Models.Betha;

namespace AutomationApp.Services.Betha
{
    public class BethaAsoIntegrationService
    {
        private readonly Action<string> _logger;

        public BethaAsoIntegrationService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessSpreadsheetAsosAsync(string pathSpreadsheet)
        {
            _logger("Iniciando o carregamento e análise da planilha...");
            var listAsos = new List<BethaAsoData>();

            try
            {
                using (var workbook = new XLWorkbook(pathSpreadsheet))
                {
                    var worksheet = workbook.Worksheet(1);
                    int totalLines = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    if (totalLines <= 1)
                    {
                        _logger("⚠️ Alerta: A planilha está vazia ou sem dados para processar.");
                        return;
                    }

                    for (int l = 2; l <= totalLines; l++)
                    {
                        var line = worksheet.Row(l);
                        var statusAtual = line.Cell(12).GetString().Trim().ToUpper();

                        if (statusAtual == "SUCESSO") continue;

                        var aso = new BethaAsoData
                        {
                            LinhaPlanilha       = l,
                            Funcionario         = line.Cell(1).GetString().Trim(),
                            Cpf                 = line.Cell(2).GetString().Trim(),
                            Matricula           = line.Cell(3).GetString().Trim(),
                            Cargo               = line.Cell(4).GetString().Trim(),
                            TipoExame           = line.Cell(5).GetString().Trim(),
                            Resultado           = line.Cell(6).GetString().Trim(),
                            DataExame           = line.Cell(7).GetString().Trim(),
                            DataInicio          = line.Cell(8).GetString().Trim(),
                            MedicoExaminador    = line.Cell(9).GetString().Trim(),
                            MedicoPcmso         = line.Cell(10).GetString().Trim(),
                            PdfPath             = line.Cell(11).GetString().Trim(),
                            StatusProcessamento = "PENDENTE"
                        };
                        listAsos.Add(aso);
                    }
                }

                if (listAsos.Count == 0)
                {
                    _logger("ℹ️ Nenhuma linha pendente localizada.");
                    return;
                }

                _logger($"Total de registros pendentes: {listAsos.Count}");

                var middleware = Program.ServiceProvider?.GetService<IBethaApiService>();
                if (middleware == null) throw new Exception("O Middleware de infraestrutura 'IBethaApiService' não está registrado.");

                var submitHandler = new BethaAsoSubmitHandler();
                int sent = 0;
                int blocked = 0;

                foreach (var item in listAsos)
                {
                    _logger($"\n------------------------------------------------------------------");
                    _logger($"▶ Processando Linha {item.LinhaPlanilha}: {item.Funcionario} (CPF: {item.Cpf})");

                    try
                    {
                        if (string.IsNullOrWhiteSpace(item.Cpf))
                            throw new Exception("Divergência: CPF está em branco na planilha.");

                        string cpfLimpo = item.Cpf.Replace(".", "").Replace("-", "").Trim();
                        if (cpfLimpo.Length != 11)
                            throw new Exception($"Divergência: O CPF informado [{item.Cpf}] possui tamanho inválido.");

                        if (Program.Settings.Betha?.Api?.Endpoints == null || 
                            !Program.Settings.Betha.Api.Endpoints.TryGetValue("RegistrationList", out string? endpointBuscaCpf))
                            throw new Exception("Erro de Configuração: Rota 'RegistrationList' ausente no appsettings.");

                        string filtroStr = $"pessoa.cpf = \"{cpfLimpo}\"";
                        string queryString = $"filter={Uri.EscapeDataString(filtroStr)}&filtroSituacao=TODOS&limit=20&offset=0";

                        _logger("-> Consultando ID interno do funcionário via Middleware...");
                        string? jsonCpfResult = await middleware.SendGetRequestAsync(endpointBuscaCpf, queryString);

                        string? funcionarioIdReal = null;
                        if (!string.IsNullOrEmpty(jsonCpfResult))
                        {
                            using JsonDocument doc = JsonDocument.Parse(jsonCpfResult);
                            JsonElement root = doc.RootElement;
                            if (root.TryGetProperty("content", out JsonElement contentArray) && 
                                contentArray.ValueKind == JsonValueKind.Array && contentArray.GetArrayLength() > 0)
                                if (contentArray[0].TryGetProperty("id", out JsonElement idProp))
                                    funcionarioIdReal = idProp.ToString();
                        }

                        if (string.IsNullOrEmpty(funcionarioIdReal))
                            throw new Exception($"Divergência: Funcionário não localizado no banco da Betha para o CPF {item.Cpf}.");

                        _logger($"-> ID localizado: {funcionarioIdReal}. Acionando Pipeline de Envio...");

                        bool sucessoEnvio = await submitHandler.SubmitSingleAsoAsync(item, funcionarioIdReal);

                        if (!sucessoEnvio)
                            throw new Exception("Falha nas validações de negócio ou na transmissão das etapas do ASO.");

                        item.StatusProcessamento = "SUCESSO";
                        item.MotivoStatus = $"ASO integrado com sucesso.";
                        sent++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        _logger($"❌ [Linha {item.LinhaPlanilha} Abortada]: {ex.Message}");
                        Console.ResetColor();

                        item.StatusProcessamento = "ERRO";
                        item.MotivoStatus = ex.Message;
                        blocked++;
                    }

                    try
                    {
                        using (var workbook = new XLWorkbook(pathSpreadsheet))
                        {
                            var worksheet = workbook.Worksheet(1);
                            var linhaExcel = worksheet.Row(item.LinhaPlanilha);

                            if (item.StatusProcessamento == "SUCESSO")
                            {
                                linhaExcel.Cell(12).Value = "SUCESSO";
                                linhaExcel.Cell(13).Value = item.MotivoStatus;
                                linhaExcel.Cell(12).Style.Fill.BackgroundColor = XLColor.LightGreen;
                                linhaExcel.Cell(13).Style.Font.FontColor = XLColor.Black;
                            }
                            else
                            {
                                linhaExcel.Cell(12).Value = "ERRO";
                                linhaExcel.Cell(13).Value = item.MotivoStatus;
                                linhaExcel.Cell(12).Style.Fill.BackgroundColor = XLColor.LightPink;
                                linhaExcel.Cell(13).Style.Font.FontColor = XLColor.Red;
                            }
                            workbook.Save();
                        }
                    }
                    catch (Exception exExcel)
                    {
                        _logger($"⚠️ Alerta crítica: Falha ao salvar status da linha {item.LinhaPlanilha} no Excel: {exExcel.Message}");
                    }
                }

                Console.WriteLine();
                _logger("==================================================================");
                _logger($"   RELATÓRIO DO LOTE CONCLUÍDO:");
                _logger($"   - Total de registros na fila : {listAsos.Count}");
                _logger($"   - Processados com Sucesso    : {sent}");
                _logger($"   - Ignorados ou com Erro      : {blocked}");
                _logger("==================================================================");
            }
            catch (Exception exCritico)
            {
                _logger($"❌ Erro fatal no motor de lote do serviço: {exCritico.Message}");
            }
        }
    }
}