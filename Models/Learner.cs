using Eloi.Services.Documents;
using Eloi.Services.Http;
using Eloi.Services.RetrievalAugmentation;
using Eloi.Utility;

namespace Eloi.Models {
    public sealed class KnowledgeIngestor {
        private readonly BookLover _bookLover;
        private readonly EloiClient _eloiClient;
        private readonly RetrievalAugmentService _ras;

        public KnowledgeIngestor(BookLover booklover, EloiClient eloiClient, RetrievalAugmentService ras) {
            _bookLover = booklover;
            _eloiClient = eloiClient;
            _ras = ras;
        }

        public async Task IngestPdfAsync(string pdfPath, CancellationToken ct) {
            string text = _bookLover.ExtractText(pdfPath);

            // If text is empty, you’d OCR images of pages instead (not shown: PDF->images)
            if (string.IsNullOrWhiteSpace(text)) {
                throw new InvalidOperationException("PDF has no extractable text. Use OCR pipeline for scanned PDFs.");
            }

            await IngestTextAsync(sourceId: $"pdf:{Path.GetFileName(pdfPath)}", text, ct);
        }

        public async Task IngestWebAsync(string url, CancellationToken ct) {
            string text = await WebReach.ExtractTextFromUrlAsync(url, ct);
            await IngestTextAsync(sourceId: $"url:{url}", text, ct);
        }

        /// <summary>
        /// Processes the specified text by dividing it into chunks, generating embeddings for each chunk, and storing
        /// the results associated with the given source identifier.
        /// </summary>
        /// <remarks>Each chunk of the input text is embedded and upserted individually. The operation is
        /// performed asynchronously and can be cancelled via the provided cancellation token.</remarks>
        /// <param name="sourceId">The unique identifier for the source to associate with the ingested text chunks and their embeddings.</param>
        /// <param name="text">The text content to be chunked, embedded, and ingested. Cannot be null.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous ingest operation.</returns>
        private async Task IngestTextAsync(string sourceId, string text, CancellationToken ct) {
            foreach ((int idx, string? chunk) in Tokenizer.ChunkByChars(text)) {
                float[] emb = await _eloiClient.EmbedAsync(chunk, ct);
                _ras.Upsert(sourceId, idx, chunk, emb);
            }
        }
    }
}
