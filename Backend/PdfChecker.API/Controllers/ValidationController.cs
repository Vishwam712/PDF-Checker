using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfChecker.API.Data;
using PdfChecker.API.Model;

namespace PdfChecker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ValidationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/validation
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rules = await _context.ValidationRules.ToListAsync();
            return Ok(rules);
        }

        // POST: api/validation
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ValidationRule rule)
        {
            if (string.IsNullOrEmpty(rule.RuleName))
                return BadRequest(new { message = "RuleName is required" });

            _context.ValidationRules.Add(rule);
            await _context.SaveChangesAsync();

            return Ok(rule);
        }

        // PUT: api/validation/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ValidationRule rule)
        {
            var existing = await _context.ValidationRules.FindAsync(id);
            if (existing == null) return NotFound();

            existing.RuleName = rule.RuleName;
            existing.Description = rule.Description;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/validation/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.ValidationRules.FindAsync(id);
            if (existing == null) return NotFound();

            _context.ValidationRules.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rule deleted" });
        }
    }
}
