using Microsoft.EntityFrameworkCore;
using PdfChecker.API.Model;

namespace PdfChecker.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PdfFile> PdfFiles { get; set; }
        public DbSet<ValidationRule> ValidationRules { get; set; }
    }
}
