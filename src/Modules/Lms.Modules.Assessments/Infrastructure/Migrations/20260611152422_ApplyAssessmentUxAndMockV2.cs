using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Assessments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyAssessmentUxAndMockV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                schema: "assessments",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "DifficultyFilter",
                schema: "assessments",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                schema: "assessments",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                schema: "assessments",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedQuestionIdsJson",
                schema: "assessments",
                table: "Attempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "QuestionIdsJson",
                schema: "assessments",
                table: "Attempts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedQuestionIdsJson",
                schema: "assessments",
                table: "MockExamAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_UnitId_Type",
                schema: "assessments",
                table: "Quizzes",
                columns: new[] { "UnitId", "Type" });

            migrationBuilder.AddColumn<decimal>(
                name: "MarksPerCorrect",
                schema: "assessments",
                table: "MockExams",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyPerWrong",
                schema: "assessments",
                table: "MockExams",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MockExamSections",
                schema: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SectionTimeLimitMinutes = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExamSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockExamSections_MockExams_MockExamId",
                        column: x => x.MockExamId,
                        principalSchema: "assessments",
                        principalTable: "MockExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                schema: "assessments",
                table: "MockExamTopics",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrectCount",
                schema: "assessments",
                table: "MockExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WrongCount",
                schema: "assessments",
                table: "MockExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                schema: "assessments",
                table: "MockExamAttempts",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamSections_MockExamId",
                schema: "assessments",
                table: "MockExamSections",
                column: "MockExamId");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamTopics_SectionId",
                schema: "assessments",
                table: "MockExamTopics",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MockExamTopics_MockExamSections_SectionId",
                schema: "assessments",
                table: "MockExamTopics",
                column: "SectionId",
                principalSchema: "assessments",
                principalTable: "MockExamSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.Sql(@"
                INSERT INTO assessments.MockExamSections (Id, MockExamId, Title, SortOrder, SectionTimeLimitMinutes, TenantId, CreatedAt)
                SELECT NEWID(), m.Id, N'General', 1, NULL, m.TenantId, GETUTCDATE()
                FROM assessments.MockExams m
                WHERE NOT EXISTS (
                    SELECT 1 FROM assessments.MockExamSections s WHERE s.MockExamId = m.Id);

                UPDATE t
                SET t.SectionId = s.Id
                FROM assessments.MockExamTopics t
                INNER JOIN assessments.MockExamSections s ON s.MockExamId = t.MockExamId AND s.SortOrder = 1
                WHERE t.SectionId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "SectionId",
                schema: "assessments",
                table: "MockExamTopics",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MockExamTopics_MockExamSections_SectionId",
                schema: "assessments",
                table: "MockExamTopics");

            migrationBuilder.DropTable(
                name: "MockExamSections",
                schema: "assessments");

            migrationBuilder.DropIndex(
                name: "IX_MockExamTopics_SectionId",
                schema: "assessments",
                table: "MockExamTopics");

            migrationBuilder.DropColumn(
                name: "MarksPerCorrect",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "PenaltyPerWrong",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "SectionId",
                schema: "assessments",
                table: "MockExamTopics");

            migrationBuilder.DropColumn(
                name: "CorrectCount",
                schema: "assessments",
                table: "MockExamAttempts");

            migrationBuilder.DropColumn(
                name: "WrongCount",
                schema: "assessments",
                table: "MockExamAttempts");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                schema: "assessments",
                table: "MockExamAttempts",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_UnitId_Type",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "DifficultyFilter",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "UnitId",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                schema: "assessments",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "FlaggedQuestionIdsJson",
                schema: "assessments",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "QuestionIdsJson",
                schema: "assessments",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "FlaggedQuestionIdsJson",
                schema: "assessments",
                table: "MockExamAttempts");

            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                schema: "assessments",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
