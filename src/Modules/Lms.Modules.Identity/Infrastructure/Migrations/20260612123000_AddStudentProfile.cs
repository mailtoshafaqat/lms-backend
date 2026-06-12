using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Identity.Infrastructure.Migrations;

public partial class AddStudentProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Phone",
            schema: "identity",
            table: "Users",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfileNotes",
            schema: "identity",
            table: "Users",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureUrl",
            schema: "identity",
            table: "Users",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Phone", schema: "identity", table: "Users");
        migrationBuilder.DropColumn(name: "ProfileNotes", schema: "identity", table: "Users");
        migrationBuilder.DropColumn(name: "ProfilePictureUrl", schema: "identity", table: "Users");
    }
}
