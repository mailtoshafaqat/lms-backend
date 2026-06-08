using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembersOnlyLectures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MembersOnly",
                schema: "content",
                table: "Lectures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceLiveClassId",
                schema: "content",
                table: "Lectures",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembersOnly",
                schema: "content",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "SourceLiveClassId",
                schema: "content",
                table: "Lectures");
        }
    }
}
