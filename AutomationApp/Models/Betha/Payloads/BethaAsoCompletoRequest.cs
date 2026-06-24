using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaAsoCompletoRequest
    {
        [JsonPropertyName("dataExame")]
        public string DataExame { get; set; } = string.Empty; 

        [JsonPropertyName("dataVencimento")]
        public string DataVencimento { get; set; } = string.Empty; 

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = string.Empty; 

        [JsonPropertyName("medicoExaminador")]
        public string MedicoExaminador { get; set; } = string.Empty;

        [JsonPropertyName("medicoPcmso")]
        public string MedicoPcmso { get; set; } = string.Empty;
    }
}