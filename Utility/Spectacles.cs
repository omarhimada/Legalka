using Tesseract;

namespace Legalka.Utility {
    public static class Spectacles {
        // Requires traineddata files, e.g. ./tessdata/eng.traineddata
        public static string ExtractFromImage(string imagePath, string tessdataDir = "./tessdata", string lang = "eng") {
            using TesseractEngine engine = new TesseractEngine(tessdataDir, lang, EngineMode.Default);
            using Pix img = Pix.LoadFromFile(imagePath);
            using Page page = engine.Process(img);
            return page.GetText();
        }
    }
}
