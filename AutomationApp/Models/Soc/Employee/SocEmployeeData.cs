namespace AutomationApp.Models.Soc
{
    public class SocEmployeeData
    {
        public int LinhaPlanilha { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string DataNascimento { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty; 
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string MatriculaAnterior { get; set; } = string.Empty;
        public string DataAdmissao { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Lotacao { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string StatusProcessamento { get; set; } = "PENDENTE";
        public string MotivoStatus { get; set; } = string.Empty;
    }
}