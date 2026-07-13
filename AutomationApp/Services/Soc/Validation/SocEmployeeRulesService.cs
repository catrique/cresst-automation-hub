using System.Text.RegularExpressions;
using AutomationApp.Models.Soc;

namespace AutomationApp.Services.Soc
{
    public class SocEmployeeRulesService
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled
        );

        public bool ValidateAndNormalizeEmployee(SocEmployeeData item)
        {
            item.Nome = item.Nome?.Trim().ToUpper() ?? string.Empty;
            item.Email = item.Email?.Trim().ToLower() ?? string.Empty;
            item.Cargo = item.Cargo?.Trim().ToUpper() ?? string.Empty;
            item.Lotacao = item.Lotacao?.Trim().ToUpper() ?? string.Empty;
            item.Empresa = item.Empresa?.Trim().ToUpper() ?? string.Empty;
            item.MatriculaAnterior = item.MatriculaAnterior?.Trim() ?? string.Empty;

            string genderUpper = item.Sexo?.Trim().ToUpper() ?? string.Empty;
            if (genderUpper == "M" || genderUpper == "MASCULINO" || genderUpper == "MASC")
                item.Sexo = "MASCULINO";
            else if (genderUpper == "F" || genderUpper == "FEMININO" || genderUpper == "FEM")
                item.Sexo = "FEMININO";

            if (string.IsNullOrWhiteSpace(item.Nome))
            {
                item.StatusProcessamento = "ERRO_VALIDACAO";
                item.MotivoStatus = "Nome do funcionário está vazio.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Cpf))
            {
                item.StatusProcessamento = "ERRO_VALIDACAO";
                item.MotivoStatus = "CPF ausente ou vazio.";
                return false;
            }

            string numericCpf = Regex.Replace(item.Cpf, @"[^\d]", "");
            if (numericCpf.Length != 11)
            {
                item.StatusProcessamento = "ERRO_VALIDACAO";
                item.MotivoStatus = $"CPF inválido: [{item.Cpf}] precisa ter 11 dígitos.";
                return false;
            }
            item.Cpf = numericCpf;

            return true; 
        }

        public string IncrementRegistrationString(string currentRegistration)
        {
            if (string.IsNullOrWhiteSpace(currentRegistration))
                return "1/1";

            var match = Regex.Match(currentRegistration, @"^(.*)/(\d+)$");

            if (match.Success)
            {
                string basePart = match.Groups[1].Value;
                if (int.TryParse(match.Groups[2].Value, out int currentSuffix))
                    return $"{basePart}/{currentSuffix + 1}";
            }

            return $"{currentRegistration}/1";
        }
    }
}