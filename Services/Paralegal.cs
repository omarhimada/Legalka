namespace Legalka.Services {
    using System.Text;
    using Tesseract;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    public class Paralegal {
        public string Name => WomenNameGenerator.NextName();

        public static string ExtractText(string pdfPath) {
            using PdfDocument document = PdfDocument.Open(pdfPath);
            StringBuilder sb = new ();

            foreach (UglyToad.PdfPig.Content.Page page in document.GetPages()) {
                string pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                    sb.AppendLine(pageText);
            }

            return sb.ToString();
        }

        public static class WomenNameGenerator {
            private static readonly string[] FirstNames = {
                "Ava","Lina","Peggy","Athena","Christina","Autumn","Saskia","Danaiya",
                "Laasya","Amicia","Amulya","Rashmi","Jazzlyn","Kensia","Kayelyn","Kamirah",
                "Arienne","Joselle","Willetta","Sallie","Sharonica","Dannette","Lailany",
                "Loree","Skilynn","Melodie","Melodie","Christina","Reha","Kadeja","Kandis"
            };

            private static readonly string[] LastNames = {
                "Lee","Boswell","O'Brien","Danarius","Farouk","Cordaryl","Rotunda","Cordaryl",
                "Gentry","Warren","Karlin","Warren","Warren","Gentry","Rotunda","Lee",
                "Gentry","Cordaryl","Lee","Boswell","O'Brien","Cordaryl","Boswell","Rotunda"
            };

            private static readonly Random Rng = new();

            public static string NextName() {
                string first = FirstNames[Rng.Next(FirstNames.Length)];
                string last = LastNames[Rng.Next(LastNames.Length)];
                return $"{first} {last}";
            }

            public static void Demo(int count = 10) {
                for (int i = 0; i < count; i++)
                    Console.WriteLine(NextName());
            }
        }
    }
}
