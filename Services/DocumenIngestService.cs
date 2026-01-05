namespace Legalka.Services {
    public sealed class DocumentIngestService : IDocumentIngestService {
        public Task IngestPdfAsync(string fileName, Stream content, CancellationToken ct) {
            // TODO: persist to S3/local, run OCR/text extraction, chunk, embed, index
            return Task.CompletedTask;
        }

        public Task IngestUrlAsync(string url, CancellationToken ct) {
            // TODO: fetch HTML/PDF bytes, extract text, chunk, embed, index
            return Task.CompletedTask;
        }
    }
}
