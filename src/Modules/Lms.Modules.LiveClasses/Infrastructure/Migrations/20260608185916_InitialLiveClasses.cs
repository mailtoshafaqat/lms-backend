using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.LiveClasses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLiveClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "live");

            migrationBuilder.CreateTable(
                name: "LiveClasses",
                schema: "live",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    JoinUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StartUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MeetingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Passcode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveClasses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveClasses_BundleId_ScheduledStartUtc",
                schema: "live",
                table: "LiveClasses",
                columns: new[] { "BundleId", "ScheduledStartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveClasses",
                schema: "live");
        }
    }
}
