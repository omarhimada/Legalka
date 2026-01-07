namespace Eloi.Services.Documents {
    using System.Text;
    using Tesseract;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    public class BookLover {
        public const string Name = Constants._eloiName;

        /// <summary>
        /// Extracts all text content from a PDF file at the specified path.
        /// </summary>
        /// <param name="pdfPath">
        /// The full file path to the PDF document from which to extract text. Cannot be null or empty.
        /// </param>
        /// <returns>
        /// A string containing the concatenated text from all pages of the PDF. 
        /// Returns an empty string if the PDF
        /// contains no text.
        /// </returns>
        public string ExtractText(string pdfPath) {
            using PdfDocument document = PdfDocument.Open(pdfPath);
            StringBuilder sb = new ();

            foreach (UglyToad.PdfPig.Content.Page page in document.GetPages()) {
                string pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText)) {
                    sb.AppendLine(pageText);
                }
            }

            return sb.ToString();
        }
    }
}
