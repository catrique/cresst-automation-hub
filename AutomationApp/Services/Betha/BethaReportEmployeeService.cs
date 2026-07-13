using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
namespace AutomationApp.Services.Betha
{
    public class BethaReportEmployeeService
    {
        private readonly Action<string> _logger;

        public BethaReportEmployeeService(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<string> GenerateExcelEmployeeNamesBethaAsync()
        {
            var middleware = Program.ServiceProvider?.GetService<IBethaApiService>();

            if (middleware == null)
                throw new InvalidOperationException("IBethaApiService não registrado.");

            var endpoints = Program.Settings?.Betha?.Api?.Endpoints;

            if (endpoints == null || !endpoints.TryGetValue("RegistrationList", out string? endpointBuscaServidores) || string.IsNullOrWhiteSpace(endpointBuscaServidores))
                throw new InvalidOperationException("Rota 'RegistrationList' ausente no appsettings.");

            var reportsFolder = Program.Settings?.Paths?.Reports;

            if (string.IsNullOrWhiteSpace(reportsFolder))
                throw new InvalidOperationException("Caminho de relatórios não configurado.");


            string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", reportsFolder);

            Directory.CreateDirectory(baseFolder);

            string fileName = $"employees_{DateTime.Now:dd-MM-yyyy_HH-mm}.xlsx";
            string spreadsheetPath = Path.Combine(baseFolder, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Servidores");

            worksheet.Cell("A1").Value = "Nome";
            worksheet.Cell("B1").Value = "CPF";

            int offset = 0;
            int row = 2;
            bool hasNext = true;
            var existingCpfs = new HashSet<string>();

            try
            {
                while (hasNext)
                {
                    string queryString =  $"filter=&filtroSituacao=TODOS&limit=1000&offset={offset}&selecaoAvancada=&sort=";
                    _logger($"Consultando funcionários no Betha. Offset: {offset}");
                    string? jsonResult;

                    try
                    {
                        jsonResult = await middleware.SendGetRequestAsync(endpointBuscaServidores, queryString);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Erro ao consultar Betha no offset {offset}.", ex);
                    }


                    if (string.IsNullOrWhiteSpace(jsonResult))
                    {
                        _logger("Betha retornou resposta vazia.");
                        break;
                    }

                    using JsonDocument doc = JsonDocument.Parse(jsonResult);
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("content", out JsonElement content))
                        throw new Exception("Resposta da API Betha não possui a propriedade 'content'.");


                    if (root.TryGetProperty("hasNext", out JsonElement nextElement))
                        hasNext = nextElement.GetBoolean();
                    else
                        hasNext = false;

                    int inserted = 0;

                    foreach (var employee in content.EnumerateArray())
                    {
                        if (!employee.TryGetProperty("pessoa", out JsonElement pessoa))
                            continue;

                        string? name = pessoa.TryGetProperty("nome", out var nameElement)
                            ? nameElement.GetString()
                            : null;

                        string? cpf = pessoa.TryGetProperty("cpf", out var cpfElement)
                            ? cpfElement.GetString()
                            : null;

                        if (string.IsNullOrWhiteSpace(cpf) || string.IsNullOrWhiteSpace(name))
                            continue;

                        cpf = cpf.Replace(".", "")
                                 .Replace("-", "")
                                 .Trim();

                        if (string.IsNullOrWhiteSpace(cpf) || !existingCpfs.Add(cpf))
                            continue;


                        worksheet.Cell(row, 1).Value = name.Trim();
                        worksheet.Cell(row, 2).Value = cpf;
                        row++;
                        inserted++;
                    }
                    _logger($"Página processada. Registros adicionados: {inserted}. Total no Excel: {row - 2}");
                    offset += 1000;

                    if (content.GetArrayLength() == 0)
                        hasNext = false;
                }


                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(spreadsheetPath);
                _logger($"Excel criado com sucesso: {spreadsheetPath}");

                return spreadsheetPath;
            }
            catch (JsonException ex)
            {
                throw new Exception("Erro ao interpretar retorno JSON do Betha.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar Excel de funcionários.", ex);
            }
        }
    }
}