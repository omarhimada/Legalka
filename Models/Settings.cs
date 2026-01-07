
using static Eloi.Constants;

namespace Eloi.Models {
    public sealed record Settings {
        public string BaseUrl { get; init; } = _localEloiUrl;

        public string ChatModel { get; init; } = _eloi;

        public string EmbedModel { get; init; } = _nomicEmbedText;

        public string Memories { get; init; } = RetrievalAugmentSQLite._memoriesRetrievalAugmentedDb;
        public string EloiModelfile { get; init; } = _eloiModelfile;
    }
}
