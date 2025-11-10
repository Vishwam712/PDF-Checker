using iText.Kernel.Geom;

namespace PdfChecker.API.Model
{
    public class ValidationError
    {
        public int Page { get; set; }
        public string Text { get; set; } = string.Empty;
        public Rectangle Rect { get; set; } = new Rectangle(0, 0);
        public string Message { get; set; } = string.Empty;
    }
}
