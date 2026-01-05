namespace Legalchka.Models {
    /// <summary>
    /// Represents a segment of extracted content from a source document, including its text, source identifier,
    /// embedding vector, and optional metadata such as page number and title.
    /// </summary>
    /// <remarks>This record is commonly used to represent and store document fragments for information
    /// retrieval, search, or citation scenarios. The combination of source identifier, chunk index, and optional
    /// metadata enables precise referencing and display of document content.</remarks>
    /// <param name="SourceId">The identifier of the source from which the chunk was extracted. This may include a prefix indicating the source
    /// type, such as "pdf:HOA_Rules_2025.pdf" or "url:SomePublicPage".</param>
    /// <param name="ChunkIndex">The zero-based index of the chunk within the source document. Used to indicate the chunk's position or order
    /// relative to other chunks from the same source.</param>
    /// <param name="Text">The textual content of the chunk as extracted from the source document.</param>
    /// <param name="Embedding">The embedding vector representing the semantic content of the chunk. Typically used for similarity search or
    /// machine learning applications.</param>
    /// <param name="PageNumber">The page number in the source document where the chunk was found, if applicable. May be null if the source does
    /// not support pagination or if the page number is unknown.</param>
    /// <param name="Title">The title associated with the chunk or its source, if available. May be used for display purposes in user
    /// interfaces or for repository browsing. Can be null if no title is provided.</param>
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