using Microsoft.Data.Sqlite;
using System.Text.Json;
namespace Legalchka.Models {
    /// <summary>
    /// Provides methods for storing, updating, and searching document text chunks with associated vector embeddings in
    /// a SQLite database.
    /// </summary>
    /// <remarks>LinedPaper manages the persistence and retrieval of text chunks and their embeddings,
    /// enabling similarity-based search operations. Each chunk is uniquely identified by a source ID and chunk index.
    /// This class is intended for use with SQLite databases and is not thread-safe; concurrent access should be managed
    /// externally if required.</remarks>
    using Microsoft.Data.Sqlite;

    public sealed class SqliteRagStore {
        private readonly string _cs;

        public SqliteRagStore(string dbPath) {
            _cs = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
            Init();
        }

        private void Init() {
            using var conn = new SqliteConnection(_cs);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS chunks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_id TEXT NOT NULL,
                chunk_index INTEGER NOT NULL,
                text TEXT NOT NULL,
                embedding_json TEXT NOT NULL
            );
            """;
            cmd.ExecuteNonQuery();
        }

        public void Upsert(string sourceId, int chunkIndex, string text, float[] embedding) {
            string embJson = System.Text.Json.JsonSerializer.Serialize(embedding);

            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(_cs);
            conn.Open();

            using var cmd = conn.CreateCommand();
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

        public void InsertChunk(string sourceId, int chunkIndex, string text, string embeddingJson) {
            using var conn = new SqliteConnection(_cs);
            conn.Open();

            var cmd = conn.CreateCommand();
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

        public List<(string sourceId, int chunkIndex, string text, float score)> Search(float[] q, int topK = 6) {
            using var conn = new SqliteConnection(_cs);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT source_id, chunk_index, text, embedding_json FROM chunks;";

            using var reader = cmd.ExecuteReader();
            var results = new List<(string source, int index, string text, float score)>();

            while (reader.Read()) {
                var sourceId = reader.GetString(0);
                var chunkIndex = reader.GetInt32(1);
                var text = reader.GetString(2);
                var embJson = reader.GetString(3);

                var emb = JsonSerializer.Deserialize<float[]>(embJson) ?? Array.Empty<float>();
                var score = Cosine(q, emb);

                results.Add((sourceId, chunkIndex, text, score));
            }

            return results.OrderByDescending(r => r.score).Take(topK).ToList();
        }

        private static float Cosine(float[] a, float[] b) {
            if (a.Length == 0 || a.Length != b.Length)
                return 0;

            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++) {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }

            var denom = Math.Sqrt(na) * Math.Sqrt(nb);
            return denom == 0 ? 0 : (float)(dot / denom);
        }
    }

}
