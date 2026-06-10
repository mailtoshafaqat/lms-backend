using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations;

public partial class AddMentorBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MentorDisplayName",
            schema: "platform",
            table: "TenantSettings",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "SyllabusMentorEnabled",
            schema: "platform",
            table: "Tenants",
            type: "bit",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MentorDisplayName",
            schema: "platform",
            table: "TenantSettings");

        migrationBuilder.DropColumn(
            name: "SyllabusMentorEnabled",
            schema: "platform",
            table: "Tenants");
    }
}
