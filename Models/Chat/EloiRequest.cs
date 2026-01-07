using System.Text.Json.Serialization;
using static Eloi.Constants;

namespace Eloi.Models.Classes {
    public class EloiRequest {
        [JsonPropertyName("model")]
        public string Model { get; } = _eloi;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false; 
    }
}