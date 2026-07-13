using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using AutomationApp.Models.Soc;
using AutomationApp.Services.Betha;

namespace AutomationApp.Services.Soc
{
    public class SocEmployeeIntegrationService
    {
        private readonly Action<string> _logger;
        private readonly SocEmployeeRulesService _rulesService;

        public SocEmployeeIntegrationService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rulesService = new SocEmployeeRulesService();
        }
        public async  Task<List<SocEmployeeData>> ProcessEmployeeSpreadsheetAsync(string spreadsheetPath)
        {
            _logger("Starting dynamic reading and sanitization of employee spreadsheet...");
            var validEmployees = new List<SocEmployeeData>();

            try
            {
                using (var workbook = new XLWorkbook(spreadsheetPath))
                {
                    var worksheet = workbook.Worksheet(1);
                    int totalLines = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                    if (totalLines <= 1)
                    {
                        _logger("⚠️ Alert: Spreadsheet is empty or has no data to process.");
                        return validEmployees;
                    }

                    var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var firstRow = worksheet.Row(1);
                    int lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

                    for (int col = 1; col <= lastColumn; col++)
                    {
                        string headerName = firstRow.Cell(col).GetString().Trim();
                        if (!string.IsNullOrEmpty(headerName))
                            headers[headerName] = col;
                    }

                    int GetColIndex(string name) => headers.TryGetValue(name, out int idx) ? idx : 0;

                    int nameCol = GetColIndex("Nome");
                    int regCol = GetColIndex("Matrícula Anterior") == 0 ? (GetColIndex("Matrícula") == 0 ? GetColIndex("Matricula") : GetColIndex("Matrícula")) : GetColIndex("Matrícula Anterior");
                    int cpfCol = GetColIndex("CPF");
                    int emailCol = GetColIndex("E-mail") == 0 ? GetColIndex("Email") : GetColIndex("E-mail");
                    int birthCol = GetColIndex("Data de Nascimento") == 0 ? GetColIndex("Data Nascimento") : GetColIndex("Data de Nascimento");
                    int sexCol = GetColIndex("Sexo");
                    int roleCol = GetColIndex("Cargo");
                    int sectorCol = GetColIndex("Lotação");
                    int admCol = GetColIndex("Data de Admissão") == 0 ? GetColIndex("Data Admissão") : GetColIndex("Data de Admissão");
                    int phoneCol = GetColIndex("Telefone");
                    int statusCol = GetColIndex("Status Processamento") == 0 ? GetColIndex("Status") : GetColIndex("Status Processamento");

                    if (statusCol == 0) statusCol = lastColumn + 1;


                    var apiMiddleware = Program.ServiceProvider?.GetService<IBethaApiService>();
                    if (apiMiddleware == null)
                        throw new InvalidOperationException("Infrastructure Middleware 'IBethaApiService' is not registered.");

                    var bethaEmployeeChecker = new BethaEmployeeCheckerService(apiMiddleware, _logger);

                    for (int l = 2; l <= totalLines; l++)
                    {
                        var line = worksheet.Row(l);

                        var employee = new SocEmployeeData
                        {
                            LinhaPlanilha = l,
                            Nome = nameCol > 0 ? line.Cell(nameCol).GetString() : string.Empty,
                            MatriculaAnterior = regCol > 0 ? line.Cell(regCol).GetString() : string.Empty,
                            Email = emailCol > 0 ? line.Cell(emailCol).GetString() : string.Empty,
                            Cpf = cpfCol > 0 ? line.Cell(cpfCol).GetString() : string.Empty,
                            DataNascimento = birthCol > 0 ? line.Cell(birthCol).GetString() : string.Empty,
                            Sexo = sexCol > 0 ? line.Cell(sexCol).GetString() : string.Empty,
                            Cargo = roleCol > 0 ? line.Cell(roleCol).GetString() : string.Empty,
                            Lotacao = sectorCol > 0 ? line.Cell(sectorCol).GetString() : string.Empty,
                            DataAdmissao = admCol > 0 ? line.Cell(admCol).GetString() : string.Empty,
                            Telefone = phoneCol > 0 ? line.Cell(phoneCol).GetString() : string.Empty,
                            Empresa = string.Empty,
                            StatusProcessamento = "PENDENTE",
                            MotivoStatus = string.Empty
                        };

                        bool isValid = _rulesService.ValidateAndNormalizeEmployee(employee);

                        if (!isValid)
                        {
                            line.Cell(statusCol).SetValue(employee.StatusProcessamento);
                            line.Cell(statusCol + 1).SetValue(employee.MotivoStatus);
                            _logger($"⚠️ [Row {l} Rejected]: {employee.MotivoStatus}");
                            continue;
                        }

                        if (regCol > 0)
                        {
                            string rawReg = employee.MatriculaAnterior.Trim();

                            if (string.IsNullOrWhiteSpace(rawReg) || rawReg.ToUpper() == "N/A" || rawReg.ToUpper() == "NA")
                            {
                                employee.MatriculaAnterior = $"CPF:{employee.Cpf}";
                                _logger($"  ℹ️ Registration empty for {employee.Nome}. Fallback applied: [{employee.MatriculaAnterior}]");
                            }
                            else
                            {
                                _logger($"  🔍 Checking registration availability for: {employee.Nome}...");
                                string finalRegistration = await ResolveAvailableRegistrationAsync(bethaEmployeeChecker, employee.MatriculaAnterior);
                                employee.MatriculaAnterior = finalRegistration;
                            }
                        }

                        if (nameCol > 0) line.Cell(nameCol).SetValue(employee.Nome);
                        if (regCol > 0) line.Cell(regCol).SetValue(employee.MatriculaAnterior);
                        if (emailCol > 0) line.Cell(emailCol).SetValue(employee.Email);
                        if (cpfCol > 0) line.Cell(cpfCol).SetValue(employee.Cpf);
                        if (sexCol > 0) line.Cell(sexCol).SetValue(employee.Sexo);

                        validEmployees.Add(employee);
                    }

                    workbook.Save();
                }

                _logger($"\n✅ Dynamic sanitization completed. {validEmployees.Count} employees ready for SOC web automation.");
            }
            catch (Exception ex)
            {
                _logger($"❌ Critical error in dynamic employee integration engine: {ex.Message}");
            }

            return validEmployees;
        }

        private async Task<string> ResolveAvailableRegistrationAsync(BethaEmployeeCheckerService checkerService, string originalRegistration)
        {
            string candidate = originalRegistration;
            bool alreadyExists = true;

            while (alreadyExists)
            {
                alreadyExists = await checkerService.VerificarMatriculaExisteAsync(candidate);

                if (alreadyExists)
                {
                    _logger($"    ↳ Registration [{candidate}] already exists in Betha system. Incrementing with '/'...");
                    candidate = _rulesService.IncrementRegistrationString(candidate);
                }
                else
                    _logger($"    ↳ Registration [{candidate}] is available!");
            }

            return candidate;
        }
    }
}