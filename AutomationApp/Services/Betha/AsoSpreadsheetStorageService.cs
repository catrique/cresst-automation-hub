using ClosedXML.Excel;

namespace AutomationApp.Services.Betha
{
    public class AsoSpreadsheetStorageService
    {
        private readonly Action<string> _logger;

        public AsoSpreadsheetStorageService(Action<string> logger)
        {
            _logger = logger ?? (msg => { });
        }

        /// <summary>
        /// Atualiza as colunas de Status e Observação de uma linha específica na planilha.
        /// </summary>
        /// <param name="pathSpreadsheet">Caminho completo do arquivo Excel (.xlsx)</param>
        /// <param name="linhaPlanilha">Número físico da linha (Ex: 2, 3, 12...)</param>
        /// <param name="status">O status que será gravado (Ex: "SUCESSO", "JA_CADASTRADO", "ERRO")</param>
        /// <param name="observacao">O texto descritivo do motivo ou erro</param>
        public void AtualizarStatusLinha(string pathSpreadsheet, int linhaPlanilha, string status, string observacao)
        {
            try
            {
                using (var workbook = new XLWorkbook(pathSpreadsheet))
                {
                    var worksheet = workbook.Worksheet(1);
                    var linhaExcel = worksheet.Row(linhaPlanilha);
                    linhaExcel.Cell(12).Value = status.Trim().ToUpper();
                    linhaExcel.Cell(13).Value = observacao ?? string.Empty;
                    ConfigurarEstiloStatus(linhaExcel.Cell(12), linhaExcel.Cell(13), status);
                    workbook.Save();
                }
            }
            catch (Exception ex)
            {
                _logger($"⚠️ Erro crítico ao tentar gravar na planilha (Linha {linhaPlanilha}): {ex.Message}");
                throw;
            }
        }

        private void ConfigurarEstiloStatus(IXLCell celulaStatus, IXLCell celulaObs, string status)
        {
            status = status.Trim().ToUpper();

            if (status == "SUCESSO")
            {
                celulaStatus.Style.Fill.BackgroundColor = XLColor.LightGreen;
                celulaObs.Style.Font.FontColor = XLColor.Green;
            }
            else if (status == "JA_CADASTRADO" || status == "JA_EXISTE")
            {
                celulaStatus.Style.Fill.BackgroundColor = XLColor.LightYellow;
                celulaObs.Style.Font.FontColor = XLColor.Brown;
            }
            else if (status == "ERRO" || status == "MATRICULA_INVALIDA" || status == "DATA_PLANILHA_INVALIDA")
            {
                celulaStatus.Style.Fill.BackgroundColor = XLColor.LightPink;
                celulaObs.Style.Font.FontColor = XLColor.Red;
            }
        }
    }
}