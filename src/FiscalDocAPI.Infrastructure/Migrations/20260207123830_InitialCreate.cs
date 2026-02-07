using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalDocAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmitterCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    EmitterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmitterUF = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    RecipientCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    XmlContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    XmlHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_CreatedAt",
                table: "FiscalDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_DocumentKey",
                table: "FiscalDocuments",
                column: "DocumentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_EmitterCnpj",
                table: "FiscalDocuments",
                column: "EmitterCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_EmitterUF",
                table: "FiscalDocuments",
                column: "EmitterUF");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_IssueDate",
                table: "FiscalDocuments",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDocuments_XmlHash",
                table: "FiscalDocuments",
                column: "XmlHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalDocuments");
        }
    }
}
