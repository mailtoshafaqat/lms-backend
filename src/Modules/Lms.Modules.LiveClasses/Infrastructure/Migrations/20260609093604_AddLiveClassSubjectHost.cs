using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.LiveClasses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveClassSubjectHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HostName",
                schema: "live",
                table: "LiveClasses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "HostUserId",
                schema: "live",
                table: "LiveClasses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                schema: "live",
                table: "LiveClasses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SubjectTitle",
                schema: "live",
                table: "LiveClasses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostName",
                schema: "live",
                table: "LiveClasses");

            migrationBuilder.DropColumn(
                name: "HostUserId",
                schema: "live",
                table: "LiveClasses");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                schema: "live",
                table: "LiveClasses");

            migrationBuilder.DropColumn(
                name: "SubjectTitle",
                schema: "live",
                table: "LiveClasses");
        }
    }
}
