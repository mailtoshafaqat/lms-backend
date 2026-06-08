using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Polish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CustomDomain",
                schema: "platform",
                table: "Tenants",
                column: "CustomDomain",
                unique: true,
                filter: "[CustomDomain] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_CustomDomain",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "CustomDomain",
                schema: "platform",
                table: "Tenants");
        }
    }
}
