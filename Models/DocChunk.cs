namespace Eloi.Models {
    /// <summary>
    /// Represents a segment of source content along with its associated metadata and vector embedding for use in
    /// document processing or retrieval tasks.
    /// </summary>
    /// <remarks>This record is typically used in scenarios involving document indexing, semantic search, or
    /// content retrieval where both the original text and its vector representation are required. The combination of
    /// source identifier, chunk index, and optional metadata enables precise tracking and retrieval of content segments
    /// across diverse sources.</remarks>
    /// <param name="SourceId">The identifier of the source from which the chunk was extracted. This may include a prefix indicating the source
    /// type, such as "pdf:HOA_Rules_2025.pdf" or "url:SomePublicPage".</param>
    /// <param name="ChunkIndex">The zero-based index of the chunk within the source, indicating its position relative to other chunks from the
    /// same source.</param>
    /// <param name="Text">The textual content of the chunk.</param>
    /// <param name="Embedding">The vector embedding representing the semantic content of the chunk. Used for similarity search or machine
    /// learning tasks.</param>
    /// <param name="PageNumber">The page number in the source document where the chunk appears, if applicable; otherwise, <see
    /// langword="null"/>.</param>
    /// <param name="Title">The title or heading associated with the chunk, if available; otherwise, <see langword="null"/>.</param>
    public sealed record DocChunk(
        // e.g. "pdf:HOA_Rules_2025.pdf" or "url:SomePublicPage"
        string SourceId,   
        int ChunkIndex,
        string Text,
        float[] Embedding,
        int? PageNumber = null, 
        string? Title = null
    );

}