using HtmlAgilityPack;

namespace Legalka.Utility {
    public static class WebLeg {
        public static async Task<string> ExtractTextFromUrlAsync(string url, CancellationToken ct) {
            using HttpClient http = new HttpClient();
            string html = await http.GetStringAsync(url, ct);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script/style
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
                node.Remove();

            string text = doc.DocumentNode.InnerText;
            return HtmlEntity.DeEntitize(text);
        }
    }
}
