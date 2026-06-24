using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaAsoParcialRequest
    {
        [JsonPropertyName("pessoa")]
        public string PessoaId { get; set; } = string.Empty;

        [JsonPropertyName("tipoExame")]
        public string TipoExame { get; set; } = string.Empty; 
    }
}