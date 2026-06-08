using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Progress.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMistakeDiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MistakeEntries",
                schema: "progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuizTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Stem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSelectedKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimesWrong = table.Column<int>(type: "int", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MistakeEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MistakeEntries_UserId_QuestionId",
                schema: "progress",
                table: "MistakeEntries",
                columns: new[] { "UserId", "QuestionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MistakeEntries",
                schema: "progress");
        }
    }
}
