using System;
using System.Globalization;
using System.Text;

namespace AutomationApp.Services.Betha
{
    public class AsoBusinessRulesService
    {
        public string NormalizeExamType(string excelExamtype)
        {
            if (string.IsNullOrWhiteSpace(excelExamtype))
            {
                throw new ArgumentException("O tipo de exame está em branco na planilha.");
            }

            string text = excelExamtype.ToUpper().Trim();

            text = RemoveAccents(text);

            if (text.Contains("ADMISSIONAL")) return "ADMISSIONAL";
            if (text.Contains("DEMISSIONAL")) return "DEMISSIONAL";
            if (text.Contains("PERIODICO")) return "PERIODICO"; 
            if (text.Contains("RETORNO")) return "RETORNO_TRABALHO";
            if (text.Contains("MUDANCA") || text.Contains("FUNCAO")) return "MUDANCA_FUNCAO";

            throw new FormatException($"O tipo de exame '{excelExamtype.Trim()}' não é reconhecido pelo sistema.");
        }

        public (DateTime examDate, DateTime expirationDate) CalculateDates(string examDateStr, string normalizedExamType, string resultExcel)
        {
            if (string.IsNullOrWhiteSpace(examDateStr))
                throw new FormatException("A data do exame está em branco ou nula na planilha.");

            DateTime examDate = DateTime.ParseExact(examDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

            if (!DateTime.TryParse(examDateStr, new CultureInfo("pt-BR"), DateTimeStyles.None, out examDate))
            {
                if (!DateTime.TryParse(examDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out examDate))
                {
                    throw new FormatException($"A data do exame '{examDateStr}' não está em um formato válido (dd/MM/yyyy).");
                }
            }

            DateTime expirationDate;
            string result = resultExcel.ToUpper().Trim();

            if (normalizedExamType == "DEMISSIONAL")
                expirationDate = examDate.AddMonths(3);
            else if (normalizedExamType == "RETORNO_TRABALHO" && result.Contains("INAPTO"))
                expirationDate = examDate.AddMonths(3);
            else
                expirationDate = examDate.AddYears(1);

            return (examDate, expirationDate);
        }

        private string RemoveAccents(string text)
        {
            string formD = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char ch in formD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}