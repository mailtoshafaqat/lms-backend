using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations;

public partial class AddRequestIncidents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RequestIncidents",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TraceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Method = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Path = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                StatusCode = table.Column<int>(type: "int", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                ExceptionType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                TenantSlug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                DurationMs = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RequestIncidents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RequestIncidents_CreatedAt",
            schema: "platform",
            table: "RequestIncidents",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_RequestIncidents_TraceId",
            schema: "platform",
            table: "RequestIncidents",
            column: "TraceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RequestIncidents",
            schema: "platform");
    }
}
