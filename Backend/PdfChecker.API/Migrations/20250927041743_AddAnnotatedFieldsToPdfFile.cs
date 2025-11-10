using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfChecker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnotatedFieldsToPdfFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnnotatedFileName",
                table: "PdfFiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AnnotatedFilePath",
                table: "PdfFiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnotatedFileName",
                table: "PdfFiles");

            migrationBuilder.DropColumn(
                name: "AnnotatedFilePath",
                table: "PdfFiles");
        }
    }
}
