using Eloi.Models;
using Eloi.Models.Classes;
using Ollama;
using OllamaSharp;
using OllamaSharp.Models;
using System;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Eloi.Services.Http {
    public sealed class EloiClient {
        private readonly HttpClient _http;
        private readonly Settings _opt;

        public EloiClient(HttpClient http, Settings opt) {
            _http = http;
            _opt = opt;
        }

        public async Task<EloiResponse?> UpsertChunkAsync(EloiRequest request, CancellationToken cancellationToken) {
            HttpResponseMessage responseMessage = await _http.PostAsJsonAsync(
                Constants.RetrievalAugmentSQLite._upsertChunkCommandText,
                request
            );

            if (!responseMessage.IsSuccessStatusCode) {
                return new EloiResponse {
                    Done = true
                };
            }

            EloiResponse? result = await responseMessage.Content.ReadFromJsonAsync<EloiResponse>(cancellationToken);

            return result;
        }

        public async Task<string> ChatAsync(string question, string contextBlock, CancellationToken ct) {
            var payload = new {
                model = Constants._eloi, // the model built from your Modelfile
                stream = false,
                messages = new[]
                {
                    new { role = "user", content = $"CONTEXT:\n{contextBlock}\n\nQUESTION:\n{question}" }
                }
            };

            using var resp = await _http.PostAsJsonAsync(Constants._localEloiApiChatUri, payload, ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException("Ollama chat call failed.");

            var json = await resp.Content.ReadFromJsonAsync<OllamaChatReply>(ct);
            return json?.message?.content ?? "";
        }

        private sealed class OllamaChatReply {
            public Msg? message { get; set; }
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct) {
            var req = new { model = _opt.EmbedModel, input = text };
            using HttpResponseMessage resp = await _http.PostAsJsonAsync("/api/embeddings", req, ct);
            resp.EnsureSuccessStatusCode();

            EmbedResponse? json = await resp.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
            return json?.embedding ?? throw new InvalidOperationException("No embedding returned.");
        }

        private sealed class EmbedResponse { public float[] embedding { get; set; } = Array.Empty<float>(); }
        private sealed class Msg { public string content { get; set; } = ""; }
    }
}
