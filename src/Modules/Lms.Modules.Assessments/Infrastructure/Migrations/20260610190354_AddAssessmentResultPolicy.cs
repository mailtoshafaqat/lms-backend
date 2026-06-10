using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Assessments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentResultPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchCompleteThresholdPercent",
                schema: "assessments",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<bool>(
                name: "BatchNotifySent",
                schema: "assessments",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyTeachersOnBatchComplete",
                schema: "assessments",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ResultVisibility",
                schema: "assessments",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultsPublishedAtUtc",
                schema: "assessments",
                table: "Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowExplanations",
                schema: "assessments",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchCompleteThresholdPercent",
                schema: "assessments",
                table: "MockExams",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<bool>(
                name: "BatchNotifySent",
                schema: "assessments",
                table: "MockExams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyTeachersOnBatchComplete",
                schema: "assessments",
                table: "MockExams",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultVisibility",
                schema: "assessments",
                table: "MockExams",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultsPublishedAtUtc",
                schema: "assessments",
                table: "MockExams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowExplanations",
                schema: "assessments",
                table: "MockExams",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchCompleteThresholdPercent",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "BatchNotifySent",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "NotifyTeachersOnBatchComplete",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ResultVisibility",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ResultsPublishedAtUtc",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ShowExplanations",
                schema: "assessments",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "BatchCompleteThresholdPercent",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "BatchNotifySent",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "NotifyTeachersOnBatchComplete",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "ResultVisibility",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "ResultsPublishedAtUtc",
                schema: "assessments",
                table: "MockExams");

            migrationBuilder.DropColumn(
                name: "ShowExplanations",
                schema: "assessments",
                table: "MockExams");
        }
    }
}
