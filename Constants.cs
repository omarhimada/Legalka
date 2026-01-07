namespace Eloi {
    public static class Constants {
        public const string _eloiName = "Eloi";
        public const string _eloi = "eloi";
        public const string _eloiModelfile = "eloi_Modelfile";
        public const string _eloiSettingsSection = "EloiSettings";
        public const string _appSettingsMemoriesSection = "Memories";
        public const string _applicationJsonContentType = "application/json";

        public const string _localEloiUrl = "http://127.0.0.1:11434";
        public const string _localEloiApiGenerateUri = "/api/generate";
        public const string _localEloiApiChatUri = "/api/chat";

        public const string _ollamaExecutableName = "ollama";
        public const string _ollamaCreateArgument = "create eloi -f";

        public const string _dot = " ⋅ ";

        public const string _nomicEmbedText = "nomic-embed-text";
        public const string _hashedModelfile = ".eloi_modelfile.hash";

        public static class Web {
            public const string _errorPagePath = "/Error";
            public const string _notFoundPagePath = "/not-found";
        }

        public static class RetrievalAugmentSQLite {
            public const string _memoriesRetrievalAugmentedDb = "eloi_memories.db";

            public const string _upsertChunkCommandText =
            """
            INSERT INTO chunks (source_id, chunk_index, text, embedding_json)
            VALUES ($sid, $idx, $text, $emb)
            ON CONFLICT(source_id, chunk_index)
            DO UPDATE SET
                text = excluded.text,
                embedding_json = excluded.embedding_json;
            """;

        }

        public static class Messaging {
            public const string _invalidRequestPromptMissing = "Invalid request. 'prompt' is required.";
        }
    }
}
