using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.SyllabusMentor.Infrastructure.Migrations;

public partial class AddSyllabusMentor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "mentor");

        migrationBuilder.CreateTable(
            name: "KnowledgeChunks",
            schema: "mentor",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChunkIndex = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_KnowledgeChunks", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_KnowledgeChunks_TenantId",
            schema: "mentor",
            table: "KnowledgeChunks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_KnowledgeChunks_TopicId_SubjectId",
            schema: "mentor",
            table: "KnowledgeChunks",
            columns: new[] { "TopicId", "SubjectId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "KnowledgeChunks", schema: "mentor");
    }
}
