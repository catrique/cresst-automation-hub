using ClosedXML.Excel;
using AutomationApp.Models.Betha;

namespace AutomationApp.Services.Betha
{
    public class BethaAsoIntegrationService
    {
        private readonly Action<string> _logger;

        public BethaAsoIntegrationService(Action<string> logger)
        {
            _logger = logger;
        }

        public async Task ProcessSpreadsheetAsosAsync(string pathSpreadsheet)
        {
            _logger("Iniciando o carregamento dos dados da planilha...");
            var listAsos = new List<BethaAsoData>();

            try
            {
                using (var workbook = new XLWorkbook(pathSpreadsheet))
                {
                    var worksheet = workbook.Worksheet(1);
                    int totalLines = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    if (totalLines <= 1)
                    {
                        _logger("⚠️ Alerta: A planilha parece estar vazia.");
                        return;
                    }

                    for (int l = 2; l <= totalLines; l++)
                    {
                        var line = worksheet.Row(l);
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
                            
                            StatusProcessamento = line.Cell(12).GetString().Trim() 
                        };

                        if (!string.IsNullOrWhiteSpace(aso.Funcionario) || !string.IsNullOrWhiteSpace(aso.Cpf))
                        {
                            listAsos.Add(aso);
                        }
                    }
                }

                _logger($"🎉 {listAsos.Count} registros carregados da planilha. Iniciando validação e simulação...");
                Console.WriteLine();

                var rulesService = new AsoBusinessRulesService();
                var storageService = new AsoStorageService(_logger);
                var apiService = new BethaApiClientService(_logger);
                
                apiService.ConfigureAuthentication();

                int sent = 0;
                int blocked = 0;
                int alreadyProcessed = 0;

                foreach (var item in listAsos)
                {
                    if (item.StatusProcessamento == "SUCESSO")
                    {
                        alreadyProcessed++;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        _logger($"箱 [Linha {item.LinhaPlanilha}] {item.Funcionario} -> IGNORADO (Já consta como SUCESSO na planilha)");
                        Console.ResetColor();
                        continue;
                    }

                    try
                    {
                        string tipoNormalizado = rulesService.NormalizeExamType(item.TipoExame);
                        var (dtExame, dtVencimento) = rulesService.CalculateDates(item.DataExame, tipoNormalizado, item.Resultado);

                        storageService.ValidatePdfFile(item.PdfPath);

                        string asoId = await apiService.ProcessBethaStreamAsync(
                            item.Funcionario, 
                            item.Cpf, 
                            tipoNormalizado,
                            dtExame.ToString("yyyy-MM-dd"),
                            dtVencimento.ToString("yyyy-MM-dd"),
                            item.Resultado,
                            item.MedicoExaminador,
                            item.MedicoPcmso,
                            item.PdfPath
                        );

                        item.StatusProcessamento = "SUCESSO";
                        item.MotivoStatus = asoId;
                        sent++;

                        _logger($"✅ [Linha {item.LinhaPlanilha}] {item.Funcionario} -> PROCESSADO (ID: {asoId})");
                    }
                    catch (Exception ex)
                    {
                        item.StatusProcessamento = "ERRO";
                        item.MotivoStatus = ex.Message;
                        blocked++;

                        Console.ForegroundColor = ConsoleColor.Red;
                        _logger($"❌ [Linha {item.LinhaPlanilha}] {item.Funcionario} -> BLOQUEADO: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                _logger("\n💾 Gravando status e IDs atualizados na planilha...");
                
                using (var workbook = new XLWorkbook(pathSpreadsheet))
                {
                    var worksheet = workbook.Worksheet(1);

                    worksheet.Cell(1, 12).Value = "Status Processamento";
                    worksheet.Cell(1, 13).Value = "ID Retornado / Mensagem de Erro";

                    worksheet.Cell(1, 12).Style.Font.Bold = true;
                    worksheet.Cell(1, 13).Style.Font.Bold = true;

                    foreach (var item in listAsos)
                    {
                        if (item.StatusProcessamento == "SUCESSO" && item.MotivoStatus.StartsWith("ASO_"))
                        {
                            var linhaExcel = worksheet.Row(item.LinhaPlanilha);
                            linhaExcel.Cell(12).Value = "SUCESSO";
                            linhaExcel.Cell(13).Value = item.MotivoStatus;
                            linhaExcel.Cell(12).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }
                        else if (item.StatusProcessamento == "ERRO")
                        {
                            var linhaExcel = worksheet.Row(item.LinhaPlanilha);
                            linhaExcel.Cell(12).Value = "ERRO";
                            linhaExcel.Cell(13).Value = item.MotivoStatus;
                            linhaExcel.Cell(12).Style.Fill.BackgroundColor = XLColor.LightPink;
                            linhaExcel.Cell(13).Style.Font.FontColor = XLColor.Red;
                        }
                    }

                    workbook.Save();
                }

                Console.WriteLine();
                _logger("==================================================================");
                _logger($"🏆 PROCESSO FINALIZADO!");
                _logger($"   - Total na Planilha  : {listAsos.Count}");
                _logger($"   - Pulados (Já Enviados): {alreadyProcessed}");
                _logger($"   - Novos Enviados     : {sent}");
                _logger($"   - Novos Erros        : {blocked}");
                _logger("==================================================================");

            }
            catch (Exception ex)
            {
                _logger($"❌ Erro crítico no lote de processamento: {ex.Message}");
            }
        }
    }
}