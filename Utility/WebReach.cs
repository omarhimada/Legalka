using HtmlAgilityPack;

namespace Eloi.Utility {
    /// <summary>
    /// Provides utility methods for extracting plain text content from web pages.
    /// </summary>
    /// <remarks>The WebReach class is static and cannot be instantiated. All members are thread-safe and
    /// intended for use in asynchronous workflows involving web content extraction.</remarks>
    public static class WebReach {
        public static async Task<string> ExtractTextFromUrlAsync(string url, CancellationToken ct) {
            using HttpClient http = new();
            string html = await http.GetStringAsync(url, ct);

            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Remove script/style
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
                node.Remove();

            string text = doc.DocumentNode.InnerText;
            return HtmlEntity.DeEntitize(text);
        }
    }
}
