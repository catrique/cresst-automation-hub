using AutomationApp.Models.Soc;
using ClosedXML.Excel;

namespace AutomationApp.Services.Soc.Organizing;

public class AsoExcelReporter
{
    private static readonly string[] Headers =
    [
        "Funcionário", "CPF", "Matrícula", "Cargo", "Tipo Exame",
        "Resultado", "Data Exame", "Médico Examinador", "Médico PCMSO", "Caminho PDF", "Status Processamento", "Motivo Status"
    ];


    public void Generate(IReadOnlyList<AsoData> records, string outputFolder)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ASOs");

        WriteHeaders(ws);
        WriteRows(ws, records);

        ws.Columns().AdjustToContents();

        wb.SaveAs(Path.Combine(outputFolder, "Relatorio_Completo.xlsx"));
    }

    private static void WriteHeaders(IXLWorksheet ws)
    {
        for (int i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void WriteRows(IXLWorksheet ws, IReadOnlyList<AsoData> records)
    {
        for (int i = 0; i < records.Count; i++)
        {
            var d   = records[i];
            int row = i + 2;

            ws.Cell(row, 1).Value  = d.Funcionario;
            ws.Cell(row, 2).Value  = d.Cpf;
            ws.Cell(row, 3).Value  = d.Matricula;
            ws.Cell(row, 4).Value  = d.Cargo;
            ws.Cell(row, 5).Value  = d.TipoExame;
            ws.Cell(row, 6).Value  = d.Resultado;
            ws.Cell(row, 7).Value  = d.DataExame;
            ws.Cell(row, 8).Value  = d.MedicoExaminador;
            ws.Cell(row, 9).Value  = d.MedicoPcmso;
            ws.Cell(row, 10).Value = d.CaminhoPDF;
        }
    }
}