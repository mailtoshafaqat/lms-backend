using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Progress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureWatchProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletionCertificates",
                schema: "progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LectureWatchProgress",
                schema: "progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LectureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    PositionSec = table.Column<int>(type: "int", nullable: false),
                    LastWatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureWatchProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletionCertificates_TenantId_CertificateNumber",
                schema: "progress",
                table: "CompletionCertificates",
                columns: new[] { "TenantId", "CertificateNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompletionCertificates_UserId_BundleId",
                schema: "progress",
                table: "CompletionCertificates",
                columns: new[] { "UserId", "BundleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureWatchProgress_UserId_LectureId",
                schema: "progress",
                table: "LectureWatchProgress",
                columns: new[] { "UserId", "LectureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureWatchProgress_UserId_TopicId",
                schema: "progress",
                table: "LectureWatchProgress",
                columns: new[] { "UserId", "TopicId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletionCertificates",
                schema: "progress");

            migrationBuilder.DropTable(
                name: "LectureWatchProgress",
                schema: "progress");
        }
    }
}
