using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZoomSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZoomAccountId",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZoomClientId",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZoomClientSecret",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ZoomEnabled",
                schema: "platform",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZoomAccountId",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZoomClientId",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZoomClientSecret",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ZoomEnabled",
                schema: "platform",
                table: "TenantSettings");
        }
    }
}
