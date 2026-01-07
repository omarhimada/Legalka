namespace Eloi.Services.RetrievalAugmentation {
    using Eloi.Services.Http;
    using Microsoft.Data.Sqlite;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Retrieval-augmented service for answering questions using contextual information
    /// stored in a local SQLite database. Supports embedding, chunk management, and similarity-based search to enhance
    /// model responses.
    /// </summary>
    /// <remarks>RetrievalAugmentService enables efficient storage and retrieval of text chunks with
    /// associated embeddings, allowing for context-aware question answering. It integrates with an embedding client and
    /// a logging provider, and manages its own database schema for chunk data. The service is designed for use in
    /// scenarios where augmenting model responses with relevant retrieved context improves answer quality. All database
    /// operations are performed using a dedicated SQLite connection string, and the service ensures atomic upsert and
    /// search operations. Thread safety is not guaranteed; callers should ensure appropriate synchronization if
    /// accessing the service from multiple threads.</remarks>
    public sealed class RetrievalAugmentService {
        private readonly string _cs;

        private readonly EloiClient _eloiClient;
        private readonly ILogger<RetrievalAugmentService> _log;

        public RetrievalAugmentService(
            EloiClient eloiClient,
            ILogger<RetrievalAugmentService> log) {
            _eloiClient = eloiClient;
            _log = log;

            _cs = new SqliteConnectionStringBuilder { 
                DataSource = 
                Constants.RetrievalAugmentSQLite._memoriesRetrievalAugmentedDb 
            }.ToString();
            Init();
        }

        /// <summary>
        /// Asynchronously generates a response to the specified question using retrieval-augmented generation (RAG)
        /// with contextual information.
        /// </summary>
        /// <remarks>This method retrieves relevant context from a local database and uses it to enhance
        /// the model's response. The number of context chunks included is limited to avoid excessively large prompts.
        /// The operation is asynchronous and can be cancelled via the provided cancellation token.</remarks>
        /// <param name="question">The question to be answered. Cannot be null, empty, or consist only of whitespace.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <param name="topK">The maximum number of relevant context chunks to retrieve and include in the response. Must be greater than
        /// zero. The default is 6.</param>
        /// <returns>A string containing the generated answer to the question. Returns an empty string if the question is null,
        /// empty, or whitespace.</returns>
        public async Task<string> AskAsync(string question, CancellationToken ct, int topK = 6) {
            if (string.IsNullOrWhiteSpace(question))
                return string.Empty;

            // 1) Embed the question
            float[] qEmb = await _eloiClient.EmbedAsync(question, ct);

            // 2) Retrieve top chunks from SQLite
            var hits = Search(qEmb, topK: topK);

            // 3) Build context block (limit size to avoid prompt bloat)
            string contextBlock = BuildContextBlock(hits, maxChars: 12_000);

            _log.LogInformation("RAG hits: {Count}", hits.Count);

            // 4) Ask the model (Modelfile persona applied)
            return await _eloiClient.ChatAsync(question, contextBlock, ct);
        }

        /// <summary>
        /// Builds a formatted context block from a list of memory hits, including citations and scores, up to a
        /// specified character limit.
        /// </summary>
        /// <remarks>Hits are ordered by descending relevance score before formatting. Each entry in the
        /// context block includes the source identifier, chunk index, and score for easy citation. The output is
        /// trimmed to remove leading and trailing whitespace, and the method ensures deterministic
        /// formatting.</remarks>
        /// <param name="hits">A list of memory hits, where each hit contains a source identifier, chunk index, text, and relevance score.
        /// The list is used to construct the context block.</param>
        /// <param name="maxChars">The maximum number of characters allowed in the resulting context block. The method stops adding hits when
        /// this limit is reached.</param>
        /// <returns>A string containing the formatted context block with citations and associated text. If no hits are provided
        /// or the resulting block is empty, returns a default message indicating that no relevant memories were found.</returns>
        private static string BuildContextBlock(
            List<(string sourceId, int chunkIndex, string text, float score)> hits,
            int maxChars) {
            if (hits is null || hits.Count == 0)
                return "No relevant memories were found for this question.";

            var sb = new StringBuilder();

            foreach (var h in hits.OrderByDescending(x => x.score)) {
                // Very light formatting to make citations easy.
                sb.Append('[')
                  .Append(h.sourceId)
                  .Append('#')
                  .Append(h.chunkIndex)
                  .Append(" score=")
                  .Append(h.score.ToString("0.000"))
                  .AppendLine("]")
                  .AppendLine(h.text)
                  .AppendLine();

                if (sb.Length >= maxChars)
                    break;
            }

            // Keep it trimmed and deterministic
            string block = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(block)
                ? "No relevant memories were found for this question."
                : block;
        }
        
        /// <summary>
        /// Initializes the database schema required for storing chunk data if it does not already exist.
        /// </summary>
        /// <remarks>This method creates the 'chunks' table and a unique index on the combination of
        /// 'source_id' and 'chunk_index'. It should be called before performing any operations that depend on the
        /// existence of these database objects.</remarks>
        private void Init() {
            using SqliteConnection conn = new(_cs);
            conn.Open();

            SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS chunks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_id TEXT NOT NULL,
                chunk_index INTEGER NOT NULL,
                text TEXT NOT NULL,
                embedding_json TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_chunks_source_chunk
            ON chunks(source_id, chunk_index);
            """;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts a new chunk or updates an existing chunk in the database for the specified source and index.
        /// </summary>
        /// <remarks>If a chunk with the specified source ID and chunk index already exists, its text and
        /// embedding are updated; otherwise, a new chunk is inserted. This operation is atomic and ensures that the
        /// chunk data remains consistent.</remarks>
        /// <param name="sourceId">The unique identifier of the source to which the chunk belongs. Cannot be null or empty.</param>
        /// <param name="chunkIndex">The zero-based index of the chunk within the source. Must be non-negative.</param>
        /// <param name="text">The text content of the chunk to store or update. Cannot be null.</param>
        /// <param name="embedding">An array of floating-point values representing the embedding associated with the chunk. Cannot be null.</param>
        public void Upsert(string sourceId, int chunkIndex, string text, float[] embedding) {
            string embJson = JsonSerializer.Serialize(embedding);

            using SqliteConnection conn = new(_cs);
            conn.Open();

            using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText =
            """
            INSERT INTO chunks (source_id, chunk_index, text, embedding_json)
            VALUES ($sid, $idx, $text, $emb)
            ON CONFLICT(source_id, chunk_index)
            DO UPDATE SET
                text = excluded.text,
                embedding_json = excluded.embedding_json;
            """;

            cmd.Parameters.AddWithValue("$sid", sourceId);
            cmd.Parameters.AddWithValue("$idx", chunkIndex);
            cmd.Parameters.AddWithValue("$text", text);
            cmd.Parameters.AddWithValue("$emb", embJson);

            cmd.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Inserts a new chunk record into the database for the specified source, index, text, and embedding data.
        /// </summary>
        /// <remarks>This method adds a chunk to the database and associates it with the specified source.
        /// If a chunk with the same source and index already exists, a duplicate record will be created unless database
        /// constraints prevent it. The method opens a new database connection for each call.</remarks>
        /// <param name="sourceId">The unique identifier of the source to which the chunk belongs. Cannot be null or empty.</param>
        /// <param name="chunkIndex">The zero-based index of the chunk within the source. Must be greater than or equal to zero.</param>
        /// <param name="text">The textual content of the chunk to be stored. Cannot be null.</param>
        /// <param name="embeddingJson">A JSON string representing the embedding data associated with the chunk. Cannot be null.</param>
        public void InsertChunk(string sourceId, int chunkIndex, string text, string embeddingJson) {
            using SqliteConnection conn = new(_cs);
            conn.Open();

            SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText =
            """
            INSERT INTO chunks (source_id, chunk_index, text, embedding_json)
            VALUES ($sid, $idx, $text, $emb);
            """;
            cmd.Parameters.AddWithValue("$sid", sourceId);
            cmd.Parameters.AddWithValue("$idx", chunkIndex);
            cmd.Parameters.AddWithValue("$text", text);
            cmd.Parameters.AddWithValue("$emb", embeddingJson);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Searches for the top matching text chunks based on the provided query embedding.
        /// </summary>
        /// <remarks>The similarity score is calculated using cosine similarity between the query
        /// embedding and each chunk's embedding. Higher scores indicate greater similarity. The method reads all
        /// available chunks from the underlying SQLite database and ranks them by similarity to the query.</remarks>
        /// <param name="q">The query embedding represented as an array of floating-point values. Cannot be null.</param>
        /// <param name="topK">The maximum number of top results to return. Must be greater than zero. The default value is 6.</param>
        /// <returns>A list of tuples containing the source ID, chunk index, text, and similarity score for each of the top
        /// matching chunks. The list contains up to <paramref name="topK"/> results, ordered by descending score.</returns>
        public List<(string sourceId, int chunkIndex, string text, float score)> Search(float[] q, int topK = 6) {
            using SqliteConnection conn = new(_cs);
            conn.Open();

            SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT source_id, chunk_index, text, embedding_json FROM chunks;";

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<(string source, int index, string text, float score)> results = new();

            while (reader.Read()) {
                string sourceId = reader.GetString(0);
                int chunkIndex = reader.GetInt32(1);
                string text = reader.GetString(2);
                string embJson = reader.GetString(3);

                float[] emb = JsonSerializer.Deserialize<float[]>(embJson) ?? Array.Empty<float>();
                float score = Cosine(q, emb);

                results.Add((sourceId, chunkIndex, text, score));
            }

            return results.OrderByDescending(r => r.score).Take(topK).ToList();
        }

        /// <summary>
        /// Calculates the cosine similarity between two vectors represented as arrays of single-precision
        /// floating-point numbers.
        /// </summary>
        /// <remarks>Cosine similarity measures the cosine of the angle between two vectors, providing a
        /// metric for their directional similarity. Both input arrays must be of equal length; otherwise, the method
        /// returns 0. If either vector is a zero vector, the result is also 0.</remarks>
        /// <param name="a">The first input vector. Must have the same length as <paramref name="b"/>.</param>
        /// <param name="b">The second input vector. Must have the same length as <paramref name="a"/>.</param>
        /// <returns>A value between -1 and 1 representing the cosine similarity of the two vectors. Returns 0 if the vectors are
        /// empty or have different lengths.</returns>
        private static float Cosine(float[] a, float[] b) {
            if (a.Length == 0 || a.Length != b.Length)
                return 0;

            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++) {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }

            double denom = Math.Sqrt(na) * Math.Sqrt(nb);
            return denom == 0 ? 0 : (float)(dot / denom);
        }
    }
}
