using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.LiveClasses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveClassRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecordingLectureId",
                schema: "live",
                table: "LiveClasses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordingTopicId",
                schema: "live",
                table: "LiveClasses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordingUrl",
                schema: "live",
                table: "LiveClasses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordingLectureId",
                schema: "live",
                table: "LiveClasses");

            migrationBuilder.DropColumn(
                name: "RecordingTopicId",
                schema: "live",
                table: "LiveClasses");

            migrationBuilder.DropColumn(
                name: "RecordingUrl",
                schema: "live",
                table: "LiveClasses");
        }
    }
}
