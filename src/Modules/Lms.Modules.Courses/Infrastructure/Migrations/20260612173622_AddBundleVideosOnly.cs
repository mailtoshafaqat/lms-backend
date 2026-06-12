using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleVideosOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VideosOnly",
                schema: "courses",
                table: "Bundles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideosOnly",
                schema: "courses",
                table: "Bundles");
        }
    }
}
