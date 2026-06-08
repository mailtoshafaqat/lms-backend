using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUser = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UseSsl = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                schema: "platform",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSettings",
                schema: "platform");
        }
    }
}
