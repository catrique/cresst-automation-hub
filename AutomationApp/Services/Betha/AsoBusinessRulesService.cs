using System.Globalization;
using System.Text;

namespace AutomationApp.Services.Betha
{
    public class AsoBusinessRulesService
    {
        public string NormalizeExamType(string excelExamType)
        {
            if (string.IsNullOrWhiteSpace(excelExamType))
                throw new ArgumentException("O tipo de exame está em branco na planilha.");

            string text = RemoveAccents(excelExamType.Trim().ToUpper());

            if (text == "RETORNO AO TRABALHO") return "RETORNO_TRABALHO";
            if (text == "MUDANCA DE FUNCAO") return "MUDANCA_FUNCAO";

            return text;
        }

        public (string dataExame, string dataInicioAtividades, string dataValidade) CalcularDatas(
            string dataExameExcel,
            string resultadoExcel,
            string tipoExameNormalizado,
            string? dataInicioContrato)
        {
            if (string.IsNullOrWhiteSpace(dataExameExcel))
                throw new FormatException("A data do exame está em branco ou nula na planilha.");

            if (!DateTime.TryParseExact(dataExameExcel.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtExame))
                throw new FormatException($"A data do exame '{dataExameExcel}' não está em um formato válido (dd/MM/yyyy).");

            string dataFormatada = dtExame.ToString("yyyy-MM-dd");

            string dataInicioAtividades;
            if (tipoExameNormalizado == "DEMISSIONAL")
            {
                if (string.IsNullOrWhiteSpace(dataInicioContrato))
                    throw new Exception("Data de início de contrato (matrícula) não localizada para exame demissional.");
                dataInicioAtividades = dataInicioContrato;
            }
            else
            {
                int diasOffset = dtExame.DayOfWeek == DayOfWeek.Friday ? 3 : 1;
                dataInicioAtividades = dtExame.AddDays(diasOffset).ToString("yyyy-MM-dd");
            }

            string resultadoLimpo = resultadoExcel.Trim().ToUpper();
            DateTime dtValidade = tipoExameNormalizado == "DEMISSIONAL"
                ? dtExame.AddMonths(3)
                : resultadoLimpo == "APTO"
                    ? dtExame.AddYears(1)
                    : dtExame.AddMonths(3);

            return (dataFormatada, dataInicioAtividades, dtValidade.ToString("yyyy-MM-dd"));
        }

        private string RemoveAccents(string text)
        {
            string formD = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char ch in formD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}