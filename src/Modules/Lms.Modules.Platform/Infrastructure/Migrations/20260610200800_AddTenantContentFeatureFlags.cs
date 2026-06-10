using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations;

public partial class AddTenantContentFeatureFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "BundlePriceEditEnabled",
            schema: "platform",
            table: "Tenants",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "McqBulkImportEnabled",
            schema: "platform",
            table: "Tenants",
            type: "bit",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BundlePriceEditEnabled",
            schema: "platform",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "McqBulkImportEnabled",
            schema: "platform",
            table: "Tenants");
    }
}
