using System;

namespace PdfChecker.API.Model
{
    public class ValidationRule
    {
        public int Id { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }   
}
