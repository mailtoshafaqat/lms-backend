using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleEnrollmentCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                schema: "courses",
                table: "Bundles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentClosesAt",
                schema: "courses",
                table: "Bundles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentOpensAt",
                schema: "courses",
                table: "Bundles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxEnrollments",
                schema: "courses",
                table: "Bundles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                schema: "courses",
                table: "Bundles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndsAt",
                schema: "courses",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "EnrollmentClosesAt",
                schema: "courses",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "EnrollmentOpensAt",
                schema: "courses",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "MaxEnrollments",
                schema: "courses",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                schema: "courses",
                table: "Bundles");
        }
    }
}
