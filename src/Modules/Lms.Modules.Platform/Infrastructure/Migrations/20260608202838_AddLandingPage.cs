using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandingPages",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageSections",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandingPageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageSections_LandingPages_LandingPageId",
                        column: x => x.LandingPageId,
                        principalSchema: "platform",
                        principalTable: "LandingPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LandingPages_TenantId",
                schema: "platform",
                table: "LandingPages",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageSections_LandingPageId",
                schema: "platform",
                table: "PageSections",
                column: "LandingPageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageSections",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "LandingPages",
                schema: "platform");
        }
    }
}
