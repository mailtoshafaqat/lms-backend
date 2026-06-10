using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Assessments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFromUtc",
                schema: "assessments",
                table: "Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableUntilUtc",
                schema: "assessments",
                table: "Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                schema: "assessments",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "assessments",
                table: "Attempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                schema: "assessments",
                table: "Attempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "assessments",
                table: "Attempts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableFromUtc",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "AvailableUntilUtc",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "assessments",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "assessments",
                table: "Attempts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "assessments",
                table: "Attempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
