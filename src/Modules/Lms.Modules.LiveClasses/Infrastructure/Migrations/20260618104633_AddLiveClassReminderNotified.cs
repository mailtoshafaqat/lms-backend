using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.LiveClasses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveClassReminderNotified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderNotifiedAtUtc",
                schema: "live",
                table: "LiveClasses",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderNotifiedAtUtc",
                schema: "live",
                table: "LiveClasses");
        }
    }
}
