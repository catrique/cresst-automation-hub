using System;

namespace AutomationApp.Models.Betha
{
    public class BethaAsoData
    {
        public int LinhaPlanilha { get; set; } 
        public string Funcionario { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string TipoExame { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string DataExame { get; set; } = string.Empty;
        public string MedicoExaminador { get; set; } = string.Empty;
        public string MedicoPcmso { get; set; } = string.Empty;
        public string CaminhoPDF { get; set; } = string.Empty;
        public string StatusProcessamento { get; set; } = "PENDENTE"; 
        public string MotivoStatus { get; set; } = string.Empty;
    }
}