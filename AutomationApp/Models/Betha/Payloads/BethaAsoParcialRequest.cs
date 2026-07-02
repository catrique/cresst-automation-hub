using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaAsoParcialRequest
    {
        [JsonPropertyName("pessoa")]
        public List<string> Pessoa { get; set; } = new();

        [JsonPropertyName("tipoExame")]
        public string TipoExame { get; set; } = string.Empty; 
    }
}