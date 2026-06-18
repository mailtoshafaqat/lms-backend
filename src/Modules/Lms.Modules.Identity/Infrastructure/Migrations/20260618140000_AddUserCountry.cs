using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Identity.Infrastructure.Migrations;

public partial class AddUserCountry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Country",
            schema: "identity",
            table: "Users",
            type: "nvarchar(2)",
            maxLength: 2,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Country", schema: "identity", table: "Users");
    }
}
