using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Enrollment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "enrollment");

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "enrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PricePaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId_BundleId",
                schema: "enrollment",
                table: "Enrollments",
                columns: new[] { "UserId", "BundleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "enrollment");
        }
    }
}
