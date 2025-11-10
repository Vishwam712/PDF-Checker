using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using System.Text;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Annot;
using PdfChecker.API.Data;
using PdfChecker.API.Model;
using Org.BouncyCastle.Security;  // enables cryptography operations
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PdfChecker.API.Services
{
    public class PdfParserService
    {
        private readonly AppDbContext _context;

        public PdfParserService(AppDbContext context)
        {
            _context = context;
        }

        
        public List<ValidationError> ValidatePdf(string filePath)
        {
            // 1) Extract chunks with coordinates
            var _ = ExtractText(filePath, out var pageCoordinates);

            var result = new List<ValidationError>();

            
            bool sentenceEnded = true;  // assume we start at a new sentence
            foreach (var kv in pageCoordinates)
            {
                int page = kv.Key;
                var chunks = kv.Value
                    .OrderByDescending(c => c.Rect.GetY()) // top -> bottom
                    .ThenBy(c => c.Rect.GetX())            // left -> right
                    .ToList();

                int lineIndex = 0;
                float? lastY = null;
                const float lineThreshold = 5f; // tweak: how close Y values can be to be considered same line

                var emailRegex = new Regex(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
                var dateRegex = new System.Text.RegularExpressions.Regex(@"^(0?[1-9]|[12][0-9]|3[01])/(0?[1-9]|1[0-2])/\d{4}$");
                var cgpaRegex = new System.Text.RegularExpressions.Regex(@"^(10(\.0+)?|[1-9](\.\d+)?)$");


                // --- Rule 1: Sentence start must be capitalized ---
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    var text = (chunk.Text ?? string.Empty).Trim();

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    // detect new line (if Y differs too much)
                    if (lastY == null || Math.Abs(chunk.Rect.GetY() - lastY.Value) > lineThreshold)
                    {
                        lineIndex++;
                        lastY = chunk.Rect.GetY();
                    }

                    // If we’re at the start of a sentence, check first letter
                    if (sentenceEnded)
                    {
                        var firstChar = text.FirstOrDefault(char.IsLetter);
                        if (firstChar != default(char) && char.IsLower(firstChar))
                        {
                            // Record the error
                            result.Add(new ValidationError
                            {
                                Page = page,
                                Text = $"Line {lineIndex}: {text}",
                                Rect = chunk.Rect,
                                Message = $"Line {lineIndex} - should start with a capital letter."
                            });
                        }

                        sentenceEnded = false; // reset until we find punctuation
                    }

                    // If this chunk ends with sentence-ending punctuation, mark it
                    if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[.!?]$"))
                    {
                        sentenceEnded = true;
                    }

                    // --- RULE 2: Email format ---
                    if (text.Contains("@"))
                    {
                        // take window around chunk
                        var window = string.Concat(
                            Enumerable.Range(i - 10, 21)
                                .Where(j => j >= 0 && j < chunks.Count)
                                .Select(j => chunks[j].Text));

                        // break window into tokens (words)
                        var tokens = window.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var token in tokens)
                        {
                            if (token.Contains("@") && !emailRegex.IsMatch(token))
                            {
                                result.Add(new ValidationError
                                {
                                    Page = page,
                                    Text = token,
                                    Rect = chunk.Rect,   // highlight where @ appeared
                                    Message = $"Invalid email format near: '{token}'"
                                });
                            }
                        }
                    }

                    // --- RULE 3: Date format ---
                    if (text.Contains("/"))
                    {
                        var window = string.Concat(
                            Enumerable.Range(i - 10, 21)
                                .Where(j => j >= 0 && j < chunks.Count)
                                .Select(j => chunks[j].Text));

                        var tokens = window.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var token in tokens)
                        {
                            if (token.Contains("/") && !dateRegex.IsMatch(token))
                            {
                                result.Add(new ValidationError
                                {
                                    Page = page,
                                    Text = token,
                                    Rect = chunk.Rect,
                                    Message = $"Invalid date format near: '{token}' (expected DD/MM/YYYY)"
                                });
                            }
                        }
                    }

                    // Look for the keyword CGPA in the chunk
                    if (text.Contains("CGPA", StringComparison.OrdinalIgnoreCase))
                    {
                        // Search this chunk and the next one (if number split)
                        var window = text;
                        if (i + 1 < chunks.Count)
                            window += " " + (chunks[i + 1].Text ?? string.Empty);

                        // Find numeric pattern in window
                        var match = System.Text.RegularExpressions.Regex.Match(window, @"\d+(\.\d+)?");
                        if (match.Success)
                        {
                            var numberStr = match.Value;
                            if (!cgpaRegex.IsMatch(numberStr))
                            {
                                result.Add(new ValidationError
                                {
                                    Page = page,
                                    Text = numberStr,
                                    Rect = chunk.Rect, // highlight the chunk containing "CGPA"
                                    Message = $"Invalid CGPA: '{numberStr}' (expected between 1.0 and 10.0)"
                                });
                            }
                        }
                        else
                        {
                            // CGPA mentioned but no number found
                            result.Add(new ValidationError
                            {
                                Page = page,
                                Text = text,
                                Rect = chunk.Rect,
                                Message = "CGPA mentioned but no valid number found."
                            });
                        }
                    }
                }


            }

            return result;
        }



        // Generate highlighted copy of PDF
        public string GenerateHighlightedPdf(string inputPath, List<ValidationError> errors)
        {
            string outputPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(inputPath)!,
                $"highlighted_{System.IO.Path.GetFileNameWithoutExtension(inputPath)}_{Guid.NewGuid()}.pdf"
            );

            if (System.IO.File.Exists(outputPath))
                try { System.IO.File.Delete(outputPath); } catch { }

            using (var reader = new PdfReader(inputPath))
            using (var writer = new PdfWriter(outputPath))
            using (var pdfDoc = new PdfDocument(reader, writer))
            {
                foreach (var error in errors)
                {
                    var page = pdfDoc.GetPage(error.Page);
                    var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(
                        page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

                    var gs = new iText.Kernel.Pdf.Extgstate.PdfExtGState();
                    gs.SetFillOpacity(0.35f);

                    canvas.SaveState();
                    canvas.SetExtGState(gs);
                    canvas.SetFillColor(iText.Kernel.Colors.ColorConstants.YELLOW);

                    float x = error.Rect.GetX();
                    float y = error.Rect.GetY();
                    float w = error.Rect.GetWidth()*2;
                    float h = error.Rect.GetHeight();

                    canvas.Rectangle(x, y, w, h);
                    canvas.Fill();
                    canvas.RestoreState();

                    Console.WriteLine($"[DEBUG] Highlighted '{error.Text}' at {x},{y},{w},{h}");
                }
            }

            return outputPath;
        }


        // Helper: Extract all text from PDF using iText7
        private string ExtractText(string filePath, out Dictionary<int, List<(string Text, Rectangle Rect)>> pageCoordinates)
        {
            pageCoordinates = new Dictionary<int, List<(string, Rectangle)>>();
            var sb = new StringBuilder();

            using var pdfDoc = new PdfDocument(new PdfReader(filePath));
            int pageCount = pdfDoc.GetNumberOfPages();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDoc.GetPage(i);
                var listener = new MyLocationStrategy();
                var processor = new PdfCanvasProcessor(listener);

                // This will call listener.EventOccurred for each text render event
                processor.ProcessPageContent(page);

                // store chunks for this page
                pageCoordinates[i] = new List<(string, Rectangle)>(listener.Chunks);

                // append text (joined) for compatibility with existing validation logic
                foreach (var c in listener.Chunks)
                {
                    if (!string.IsNullOrWhiteSpace(c.Text))
                        sb.AppendLine(c.Text);
                }

                Console.WriteLine($"[DEBUG] ExtractText: Page {i} -> {listener.Chunks.Count} chunks");
            }

            return sb.ToString();
        }

        // PdfParserService.cs (add inside the PdfParserService class)
        // public string DuplicatePdf(string inputPath)
        // {
        //     if (!System.IO.File.Exists(inputPath))
        //         throw new System.IO.FileNotFoundException("Input PDF not found", inputPath);

        //     var originalName = System.IO.Path.GetFileNameWithoutExtension(inputPath);
        //     var outputFileName = $"copy_{originalName}_{Guid.NewGuid()}.pdf";
        //     var outputPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPath)!, outputFileName);

        //     // Copy using streams with explicit FileShare to avoid locks
        //     using (var src = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        //     using (var dst = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        //     {
        //         src.CopyTo(dst);
        //         dst.Flush(true);
        //     }

        //     return outputPath;
        // }

    }

}
