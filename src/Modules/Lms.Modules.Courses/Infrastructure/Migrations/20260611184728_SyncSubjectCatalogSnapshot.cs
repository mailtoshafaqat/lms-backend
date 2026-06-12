using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncSubjectCatalogSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectDefinitions",
                schema: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubjectDefinitionTeachers",
                schema: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectDefinitionTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectDefinitionTeachers_SubjectDefinitions_SubjectDefinitionId",
                        column: x => x.SubjectDefinitionId,
                        principalSchema: "courses",
                        principalTable: "SubjectDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectDefinitionId",
                schema: "courses",
                table: "Subjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SubjectId",
                schema: "courses",
                table: "Units",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectDefinitionId",
                schema: "courses",
                table: "Units",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubjectSharedUnits",
                schema: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectSharedUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectSharedUnits_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "courses",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectSharedUnits_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "courses",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectDefinitions_TenantId_Code",
                schema: "courses",
                table: "SubjectDefinitions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectDefinitionId",
                schema: "courses",
                table: "Subjects",
                column: "SubjectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_SubjectDefinitionId",
                schema: "courses",
                table: "Units",
                column: "SubjectDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectDefinitionTeachers_SubjectDefinitionId_UserId",
                schema: "courses",
                table: "SubjectDefinitionTeachers",
                columns: new[] { "SubjectDefinitionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectDefinitionTeachers_UserId",
                schema: "courses",
                table: "SubjectDefinitionTeachers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSharedUnits_SubjectId_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits",
                columns: new[] { "SubjectId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSharedUnits_UnitId",
                schema: "courses",
                table: "SubjectSharedUnits",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_SubjectDefinitions_SubjectDefinitionId",
                schema: "courses",
                table: "Subjects",
                column: "SubjectDefinitionId",
                principalSchema: "courses",
                principalTable: "SubjectDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_SubjectDefinitions_SubjectDefinitionId",
                schema: "courses",
                table: "Units",
                column: "SubjectDefinitionId",
                principalSchema: "courses",
                principalTable: "SubjectDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_Units_Subjects_SubjectId",
                schema: "courses",
                table: "Units");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Subjects_SubjectId",
                schema: "courses",
                table: "Units",
                column: "SubjectId",
                principalSchema: "courses",
                principalTable: "Subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_Subjects_SubjectId",
                schema: "courses",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_SubjectDefinitions_SubjectDefinitionId",
                schema: "courses",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_SubjectDefinitions_SubjectDefinitionId",
                schema: "courses",
                table: "Units");

            migrationBuilder.DropTable(
                name: "SubjectDefinitionTeachers",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "SubjectSharedUnits",
                schema: "courses");

            migrationBuilder.DropTable(
                name: "SubjectDefinitions",
                schema: "courses");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectDefinitionId",
                schema: "courses",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SubjectDefinitionId",
                schema: "courses",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Units_SubjectDefinitionId",
                schema: "courses",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "SubjectDefinitionId",
                schema: "courses",
                table: "Units");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubjectId",
                schema: "courses",
                table: "Units",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_Subjects_SubjectId",
                schema: "courses",
                table: "Units",
                column: "SubjectId",
                principalSchema: "courses",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
