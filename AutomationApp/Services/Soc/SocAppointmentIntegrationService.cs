using ClosedXML.Excel;
using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc
{
    public class SocAppointmentIntegrationService
    {
        private readonly Action<string> _logger;

        public SocAppointmentIntegrationService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<SocAppointmentData> ProcessAppointmentSpreadsheet(string spreadsheetPath)
        {
            _logger("📖 Iniciando a leitura e mapeamento da planilha de agendamentos...");
            var appointments = new List<SocAppointmentData>();

            try
            {
                using (var workbook = new XLWorkbook(spreadsheetPath))
                {
                    var worksheet = workbook.Worksheet(1);
                    int totalLines = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    if (totalLines <= 1)
                    {
                        _logger("⚠️ Alerta: A planilha está vazia ou não possui dados.");
                        return appointments;
                    }

                    var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var firstRow = worksheet.Row(1);
                    foreach (var cell in firstRow.CellsUsed())
                    {
                        headers[cell.GetString().Trim()] = cell.Address.ColumnNumber;
                    }

                    int nameCol = headers.ContainsKey("Nome") ? headers["Nome"] : 0;
                    int cpfCol = headers.ContainsKey("CPF") ? headers["CPF"] : 0;
                    int dataCol = headers.ContainsKey("Data Admissão") ? headers["Data Admissão"] : 0;

                    int dataAgendamentoCol = headers.ContainsKey("Data Agendamento") ? headers["Data Agendamento"] : 0;
                    int horaAgendamentoCol = headers.ContainsKey("Hora Agendamento") ? headers["Hora Agendamento"] : 0;

                    for (int rowNum = 2; rowNum <= totalLines; rowNum++)
                    {
                        var line = worksheet.Row(rowNum);
                        string cpfCru = cpfCol > 0 ? line.Cell(cpfCol).GetString().Trim() : "";

                        string cpfLimpo = System.Text.RegularExpressions.Regex.Replace(cpfCru, @"[^\d]", "");

                        if (string.IsNullOrWhiteSpace(cpfLimpo)) continue;

                        var app = new SocAppointmentData
                        {
                            LinhaPlanilha = rowNum,
                            Nome = nameCol > 0 ? line.Cell(nameCol).GetString().Trim().ToUpper() : "",
                            Cpf = cpfLimpo,
                            DataAgendamento = dataAgendamentoCol > 0 ? line.Cell(dataAgendamentoCol).GetString().Trim() : 
                                              (dataCol > 0 ? line.Cell(dataCol).GetString().Trim() : DateTime.Now.ToString("dd/MM/yyyy")),
                            HoraAgendamento = horaAgendamentoCol > 0 ? line.Cell(horaAgendamentoCol).GetString().Trim() : "08:00"
                        };

                        appointments.Add(app);
                    }
                }

                _logger($"\n✅ Leitura concluída. {appointments.Count} agendamentos carregados com sucesso.");
            }
            catch (Exception ex)
            {
                _logger($"❌ Erro crítico ao ler planilha de agendamentos: {ex.Message}");
            }

            return appointments;
        }
    }
}