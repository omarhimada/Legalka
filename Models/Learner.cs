using Legalchka.Services;
using Legalka.Services;
using Legalka.Utility;

namespace Legalchka.Models {
    public class Learner {
        public sealed class KnowledgeIngestor {
            private readonly OllamaClient _ai;
            private readonly SqliteRagStore _store;

            public KnowledgeIngestor(OllamaClient ai, SqliteRagStore store) {
                _ai = ai;
                _store = store;
            }

            public async Task IngestPdfAsync(string pdfPath, CancellationToken ct) {
                string text = Paralegal.ExtractText(pdfPath);

                // If text is empty, you’d OCR images of pages instead (not shown: PDF->images)
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("PDF has no extractable text. Use OCR pipeline for scanned PDFs.");

                await IngestTextAsync(sourceId: $"pdf:{Path.GetFileName(pdfPath)}", text, ct);
            }

            public async Task IngestWebAsync(string url, CancellationToken ct) {
                string text = await WebLeg.ExtractTextFromUrlAsync(url, ct);
                await IngestTextAsync(sourceId: $"url:{url}", text, ct);
            }

            private async Task IngestTextAsync(string sourceId, string text, CancellationToken ct) {
                foreach ((int idx, string? chunk) in Parter.ChunkByChars(text)) {
                    float[] emb = await _ai.EmbedAsync(chunk, ct);
                    _store.Upsert(sourceId, idx, chunk, emb);
                }
            }

            public async Task<string> AskAsync(string question, CancellationToken ct) {
                float[] qEmb = await _ai.EmbedAsync(question, ct);
                List<(string sourceId, int chunkIndex, string text, float score)> hits = _store.Search(qEmb, topK: 6);

                IEnumerable<(string summary, string text)> context =
                    hits.Select(h => ($"{h.sourceId}#{h.chunkIndex} (score={h.score:0.000})", h.text));

                string contextBlock = string.Join(
                    "\n\n",
                    context.Select(c => $"[{c.summary}]\n{c.text}")
                );

                return await _ai.ChatAsync(question, contextBlock, ct);
            }
        }
    }
}
