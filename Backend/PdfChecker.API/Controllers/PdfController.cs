using Microsoft.AspNetCore.Mvc;
using PdfChecker.API.Data;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Threading.Tasks;
using PdfChecker.API.Services; 
using PdfChecker.API.Model;

namespace PdfChecker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly PdfParserService _pdfParser;
        private readonly AppDbContext _context;


        public PdfController(PdfParserService pdfParser, AppDbContext context)
        {
            _pdfParser = pdfParser;
            _context = context;

        }    

        private readonly long _fileSizeLimit = 5 * 1024 * 1024; // 5 MB
        private readonly string[] _allowedExtensions = { ".pdf" };

        // GET: api/pdf/ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Backend is running successfully!" });
        }

        // POST: api/pdf/upload
        // Accepts a single file form-data field named "file"
        [HttpPost("upload")]
        // [RequestSizeLimit(20_000_000)] // ~20MB
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            if (file.Length > _fileSizeLimit)
            {
                return BadRequest(new { message = "File size exceeds 5 MB limit." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Only PDF files are allowed." });
            }

            if (file.ContentType != "application/pdf")
            {
                return BadRequest(new { message = "Invalid file type. Must be PDF." });
            }    

            // Save file to a local uploads folder (for now)
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }
            var savedFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsDir, savedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save metadata in DB
            var pdfFile = new PdfFile
            {
                OriginalFileName = file.FileName,
                StoredFileName = savedFileName,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow
            };

            _context.PdfFiles.Add(pdfFile);
            await _context.SaveChangesAsync();

            // return the saved path (for dev only). In production return a safe URL or id.
            return Ok(new { message = "File uploaded successfully.",fileId = pdfFile.Id, filename = file.FileName});
        }

        // GET: api/pdf/check/{id}
        // PdfController.cs - replace your CheckPdf action with this:
        [HttpGet("check/{id}")]
        public async Task<IActionResult> CheckPdf(int id)
        {
            var pdfFile = await _context.PdfFiles.FindAsync(id);
            if (pdfFile == null)
                return NotFound(new { message = "File not found." });

            var inputPath = pdfFile.FilePath;
            if (!System.IO.File.Exists(inputPath))
                return NotFound(new { message = "Saved file not found on server." });

            List<ValidationError> errors;
            try
            {
                // run validation logic (synchronous or Task.Run if CPU-bound)
                errors = _pdfParser.ValidatePdf(inputPath);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error parsing PDF", detail = ex.ToString() });
            }

            // If no errors, return success (optionally still return fileId)
            if (errors == null || errors.Count == 0)
            {
                return Ok(new { message = "PDF passed all checks ✅", fileId = pdfFile.Id });
            }

            // Duplicate the PDF (safe, guaranteed readable)
            string highlightedPath;
            try
            {
                highlightedPath = _pdfParser.GenerateHighlightedPdf(inputPath, errors);

                // Persist annotated/duplicated file info so frontend can fetch later
                pdfFile.AnnotatedFileName = System.IO.Path.GetFileName(highlightedPath);
                pdfFile.AnnotatedFilePath = highlightedPath;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error duplicating PDF", detail = ex.ToString() });
            }            

            // Build download URL (URL-encode filename for safety)
            var fileName = System.Net.WebUtility.UrlEncode(pdfFile.AnnotatedFileName);
            var downloadUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            return Ok(new
            {
                message = "PDF has validation errors ❌",
                errors,
                downloadUrl,
                fileId = pdfFile.Id
            });
        }


    }
}
