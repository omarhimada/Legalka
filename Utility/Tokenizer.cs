namespace Eloi.Utility {
    public class Tokenizer {
        public static IEnumerable<(int idx, string chunk)> ChunkByChars(string text, int chunkSize = 1200, int overlap = 200) {
            text = (text ?? "").Replace("\r", "\n");
            int i = 0, idx = 0;

            while (i < text.Length) {
                int len = Math.Min(chunkSize, text.Length - i);
                string chunk = text.Substring(i, len).Trim();

                if (!string.IsNullOrWhiteSpace(chunk)) {
                    yield return (idx++, chunk);
                }

                i += (chunkSize - overlap);
                if (i < 0) {
                    break;
                }
            }
        }

    }
}
