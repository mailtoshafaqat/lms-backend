using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                schema: "platform",
                table: "TenantSettings");
        }
    }
}
