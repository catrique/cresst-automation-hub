using System.Text.Json.Serialization;

namespace AutomationApp.Models.Betha.Payloads
{
    public class BethaAsoParcialResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}