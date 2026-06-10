using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Assessments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionPyqAndMockExams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPyq",
                schema: "assessments",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PyqExam",
                schema: "assessments",
                table: "Questions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PyqYear",
                schema: "assessments",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MockExamAttempts",
                schema: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    QuestionIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExamAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MockExams",
                schema: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    AvailableFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailableUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MockExamTopics",
                schema: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MockExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExamTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockExamTopics_MockExams_MockExamId",
                        column: x => x.MockExamId,
                        principalSchema: "assessments",
                        principalTable: "MockExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockExamAttempts_UserId_MockExamId",
                schema: "assessments",
                table: "MockExamAttempts",
                columns: new[] { "UserId", "MockExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_SubjectId",
                schema: "assessments",
                table: "MockExams",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamTopics_MockExamId",
                schema: "assessments",
                table: "MockExamTopics",
                column: "MockExamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockExamAttempts",
                schema: "assessments");

            migrationBuilder.DropTable(
                name: "MockExamTopics",
                schema: "assessments");

            migrationBuilder.DropTable(
                name: "MockExams",
                schema: "assessments");

            migrationBuilder.DropColumn(
                name: "IsPyq",
                schema: "assessments",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PyqExam",
                schema: "assessments",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PyqYear",
                schema: "assessments",
                table: "Questions");
        }
    }
}
