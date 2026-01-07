using Eloi.Models.Classes;
using Eloi.Services.Http;
using Eloi.Services.RetrievalAugmentation;
using Eloi.Utility;

namespace Eloi.Services.Documents {
    /// <summary>
    /// Provides services for ingesting documents from various sources, such as PDF files and web pages, by extracting
    /// their text content, generating embeddings, and storing the results for retrieval and augmentation scenarios.
    /// </summary>
    /// <remarks>This service supports asynchronous ingestion workflows and is designed to process both
    /// structured and unstructured document sources. It coordinates text extraction, chunking, embedding generation,
    /// and storage, enabling downstream retrieval-augmented applications. Thread safety and resource management depend
    /// on the underlying dependencies provided to the service.</remarks>
    public sealed class DocumentIngestService : IDocumentIngestService {
        private readonly EloiClient _eloi;
        private readonly BookLover _bookLover;
        private readonly RetrievalAugmentService _ras;

        public DocumentIngestService(EloiClient ai, RetrievalAugmentService ras, BookLover bookLover) {
            _eloi = ai;
            _ras = ras;
            _bookLover = bookLover;
        }

        /// <summary>
        /// Asynchronously ingests a PDF document by extracting its text content and processing it for further use.
        /// </summary>
        /// <param name="fileName">The name of the PDF file being ingested. Used for identification and logging purposes.</param>
        /// <param name="content">A stream containing the PDF file's content. The stream must be readable and positioned at the start of the
        /// PDF data.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous ingest operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the PDF does not contain any extractable text, such as when the document consists only of scanned
        /// images without OCR.</exception>
        public async Task IngestPdfAsync(string fileName, Stream content, CancellationToken ct) {
            // Save to temp (simple)
            string tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
            await using (var fs = File.Create(tmp)) {
                await content.CopyToAsync(fs, ct);
            }

            string text = _bookLover.ExtractText(tmp);
            if (string.IsNullOrWhiteSpace(text)) {
                throw new InvalidOperationException("Document failed to extract text. Add OCR pipeline.");
            }

            await IngestTextAsync($"Document: {fileName}", text, ct);
        }

        /// <summary>
        /// Asynchronously extracts text content from the specified URL and ingests it for further processing.
        /// </summary>
        /// <param name="url">The URL of the web page to extract and ingest. Must be a valid, absolute URL.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous ingest operation.</returns>
        public async Task IngestUrlAsync(string url, CancellationToken ct) {
            // Your WebReach idea goes here; for now assume you have a helper:
            string text = await WebReach.ExtractTextFromUrlAsync(url, ct);
            await IngestTextAsync($"url:{url}", text, ct);
        }

        /// <summary>
        /// Processes the specified text by splitting it into chunks, generating embeddings for each chunk, and storing
        /// the results associated with the given source identifier.
        /// </summary>
        /// <param name="sourceId">A unique identifier for the source to which the ingested text and its embeddings will be associated.</param>
        /// <param name="text">The text content to be ingested, chunked, and embedded.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous ingest operation.</returns>
        private async Task IngestTextAsync(string sourceId, string text, CancellationToken ct) {
            foreach ((int idx, string chunk) in ChunkByCharacters(text, chunkChars: 1200, overlapChars: 150)) {
                float[] emb = await _eloi.EmbedAsync(chunk, ct);
                _ras.Upsert(sourceId, idx, chunk, emb);
            }
        }

        /// <summary>
        /// Divides the specified text into sequential chunks of a given length, with a specified number of overlapping
        /// characters between consecutive chunks.
        /// </summary>
        /// <remarks>If chunkChars is less than or equal to overlapChars, only the first chunk is
        /// returned. The last chunk may be shorter than chunkChars if the remaining text is insufficient.</remarks>
        /// <param name="text">The input string to be divided into chunks. Cannot be null.</param>
        /// <param name="chunkChars">The maximum number of characters in each chunk. Must be greater than 0.</param>
        /// <param name="overlapChars">The number of characters to overlap between consecutive chunks. Must be greater than or equal to 0 and less
        /// than chunkChars.</param>
        /// <returns>An enumerable collection of tuples, each containing the zero-based index of the chunk and the chunk string.
        /// The collection contains all chunks extracted from the input text.</returns>
        private static IEnumerable<(int idx, string chunk)> 
            ChunkByCharacters(string text, int chunkChars, int overlapChars) {
            int i = 0, idx = 0;
            while (i < text.Length) {
                int len = Math.Min(chunkChars, text.Length - i);
                string chunk = text.Substring(i, len);
                yield return (idx++, chunk);
                i += (chunkChars - overlapChars);
                if (chunkChars <= overlapChars) {
                    break;
                }
            }
        }
    }
}
