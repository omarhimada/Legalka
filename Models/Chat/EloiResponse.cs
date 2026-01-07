using System.Text.Json.Serialization;
using static Eloi.Constants;

namespace Eloi.Models.Classes {
    public class EloiResponse {
        [JsonPropertyName("model")]
        public string Model { get; } = _eloi;

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString();

        [JsonPropertyName("response")]
        public string Response { get; set; } = _dot;

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("details")]
        public string? OptionalResponse => Response ?? FallbackLines.RandomHttpFail();
    }

    public static class FallbackLines {
        public static readonly string[] HttpFails =
        [
            "Hmm… something went wrong.",
            "Sorry, I think my wires got crossed.",
            "My wires are humming but the answer didn't make it through.",
            "Apologies, the signal got tangled somewhere.",
            "Oops — looks like the pipes coughed.",
            "Hmm.. I think the hamsters powering the HTTP wheel took a coffee break.",
            "Sorry, the connection tripped over its own feet.",
            "Hmm.. something in the chain snapped.",
            "Sorry, the message got folded into the void.",
            "Hmm.. I tried to reach the other side but the bridge blinked out."
        ];

        private static readonly Random _rng = new();

        public static string RandomHttpFail()
            => HttpFails[_rng.Next(HttpFails.Length)];
    }
}