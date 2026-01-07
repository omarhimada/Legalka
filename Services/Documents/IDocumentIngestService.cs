namespace Eloi.Services.Documents {
    public interface IDocumentIngestService {
        Task IngestPdfAsync(string fileName, Stream content, CancellationToken ct);
        Task IngestUrlAsync(string url, CancellationToken ct);

        // Optional for Google:
        // Task IngestGoogleFileAsync(string fileId, CancellationToken ct);
    }
}
