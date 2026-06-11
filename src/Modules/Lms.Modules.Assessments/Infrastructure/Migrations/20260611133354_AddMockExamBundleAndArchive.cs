using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Assessments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMockExamBundleAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BundleId",
                schema: "assessments",
                table: "MockExams",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleTitle",
                schema: "assessments",
                table: "MockExams",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "assessments",
                table: "MockExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE m
                SET m.BundleId = s.BundleId,
                    m.BundleTitle = b.Title
                FROM assessments.MockExams m
                INNER JOIN courses.Subjects s ON s.Id = m.SubjectId
                INNER JOIN courses.Bundles b ON b.Id = s.BundleId
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "BundleId",
                schema: "assessments",
                table: "MockExams",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_BundleId",
                schema: "assessments",
                table: "MockExams",
                column: "BundleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MockExams_BundleId",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "BundleId",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "BundleTitle",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "assessments",
                table: "MockExams");
        }
    }
}
