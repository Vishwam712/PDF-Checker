using System;

namespace PdfChecker.API.Model
{
    public class PdfFile
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? AnnotatedFileName { get; set; }  // e.g. "highlighted_abc.pdf"
        public string? AnnotatedFilePath { get; set; }  // server absolute path

    }
}
