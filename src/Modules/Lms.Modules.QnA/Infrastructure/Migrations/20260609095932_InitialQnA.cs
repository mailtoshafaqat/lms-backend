using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.QnA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialQnA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "qna");

            migrationBuilder.CreateTable(
                name: "DoubtThreads",
                schema: "qna",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BundleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BundleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TopicTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubtThreads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoubtMessages",
                schema: "qna",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuthorRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubtMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubtMessages_DoubtThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "qna",
                        principalTable: "DoubtThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoubtMessages_ThreadId",
                schema: "qna",
                table: "DoubtMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtThreads_Status_UpdatedAt",
                schema: "qna",
                table: "DoubtThreads",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DoubtThreads_StudentUserId",
                schema: "qna",
                table: "DoubtThreads",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubtThreads_SubjectId",
                schema: "qna",
                table: "DoubtThreads",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoubtMessages",
                schema: "qna");

            migrationBuilder.DropTable(
                name: "DoubtThreads",
                schema: "qna");
        }
    }
}
