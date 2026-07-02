using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaAsoCompletoRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("dataExame")]
        public string DataExame { get; set; } = string.Empty; 

        [JsonPropertyName("dataVencimento")]
        public string DataVencimento { get; set; } = string.Empty; 

        [JsonPropertyName("resultadoAso")]
        public string ResultadoAso { get; set; } = string.Empty; 

        [JsonPropertyName("medicoExaminador")]
        public List<string> MedicoExaminador { get; set; } = new();

        [JsonPropertyName("medicoPcmso")]
        public List<string> MedicoPcmso { get; set; } = new();

        [JsonPropertyName("instituicaoMedica")]
        public List<string> InstituicaoMedica { get; set; } = new();
    }
}