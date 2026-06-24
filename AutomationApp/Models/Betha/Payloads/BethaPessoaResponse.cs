using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaPessoaResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;
    }
}