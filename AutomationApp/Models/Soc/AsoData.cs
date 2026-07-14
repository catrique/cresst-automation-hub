namespace AutomationApp.Models.Soc;

public class AsoData
{
    public string Funcionario      { get; set; } = "";
    public string Cpf              { get; set; } = "";
    public string Matricula        { get; set; } = "";
    public string Cargo            { get; set; } = "";
    public string TipoExame        { get; set; } = "";
    public string Resultado        { get; set; } = "";
    public string DataExame        { get; set; } = "";
    public string MedicoExaminador { get; set; } = "";
    public string MedicoPcmso     { get; set; } = "";
    public string CaminhoPDF          { get; set; } = "";
    public bool   LeituraOk        { get; set; }
}