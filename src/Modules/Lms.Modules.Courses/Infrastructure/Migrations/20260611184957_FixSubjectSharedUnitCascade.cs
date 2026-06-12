using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSubjectSharedUnitCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubjectSharedUnits_Units_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits");

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectSharedUnits_Units_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits",
                column: "UnitId",
                principalSchema: "courses",
                principalTable: "Units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubjectSharedUnits_Units_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits");

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectSharedUnits_Units_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits",
                column: "UnitId",
                principalSchema: "courses",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
