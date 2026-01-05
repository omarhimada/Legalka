using Legalchka.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Legalchka.Services {
    /// <summary>
    /// Provides methods for generating text embeddings and answering questions using the OpenAI API.
    /// </summary>
    /// <remarks>This class is designed to interact with OpenAI's embedding and chat completion endpoints. It
    /// requires a valid API key and an appropriately configured HttpClient. Instances of this class are not thread-safe
    /// and should not be shared across threads without proper synchronization.</remarks>
    /// <param name="client">The HTTP client instance used to send requests to the OpenAI API. The caller is responsible for managing the
    /// lifetime of this client.</param>
    /// <param name="openAISettings">The OpenAI API settings containing authentication information, such as the API key.</param>
    public sealed class OllamaClient {
        private readonly HttpClient _http;

        public OllamaClient(HttpClient http) {
            _http = http;
            _http.BaseAddress ??= new Uri("http://localhost:11434/");
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct) {
            // Ollama embeddings endpoint
            var payload = new { model = "nomic-embed-text", prompt = text };
            using var resp = await _http.PostAsJsonAsync("api/embeddings", payload, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var arr = doc.RootElement.GetProperty("embedding");

            var vec = new float[arr.GetArrayLength()];
            int i = 0;
            foreach (var n in arr.EnumerateArray())
                vec[i++] = n.GetSingle();

            return vec;
        }

        public async Task<string> ChatAsync(string question, string context, CancellationToken ct) {
            // Non-streaming generate (simpler). You can swap to /api/chat later.
            var prompt =
            $"""
            You are a careful assistant. Answer ONLY using the context below.
            If the answer isn't in the context, say "I don't know from the provided documents."

            CONTEXT:
            {context}

            QUESTION:
            {question}

            Answer with short citations like [Source: ...].
            """;

            var payload = new { model = "llama3", prompt, stream = false };
            using var resp = await _http.PostAsJsonAsync("api/generate", payload, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("response").GetString() ?? "";
        }
    }
}
