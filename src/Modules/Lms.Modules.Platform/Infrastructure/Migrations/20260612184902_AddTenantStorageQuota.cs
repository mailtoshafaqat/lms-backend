using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantStorageQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StorageQuotaBypass",
                schema: "platform",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "StorageQuotaBytesOverride",
                schema: "platform",
                table: "Tenants",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StorageUsedBytes",
                schema: "platform",
                table: "Tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "TenantStorageObjects",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Folder = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantStorageObjects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantStorageObjects_TenantId_StorageKey",
                schema: "platform",
                table: "TenantStorageObjects",
                columns: new[] { "TenantId", "StorageKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantStorageObjects",
                schema: "platform");

            migrationBuilder.DropColumn(
                name: "StorageQuotaBypass",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StorageQuotaBytesOverride",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StorageUsedBytes",
                schema: "platform",
                table: "Tenants");
        }
    }
}
