using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lms.Modules.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPaymentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EasypaisaCredentials",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EasypaisaEnabled",
                schema: "platform",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EasypaisaHashKey",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EasypaisaStoreId",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JazzCashEnabled",
                schema: "platform",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JazzCashHashKey",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JazzCashMerchantId",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JazzCashPassword",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JazzCashReturnUrl",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ManualPaymentEnabled",
                schema: "platform",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualPaymentInstructions",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeEnabled",
                schema: "platform",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripePublishableKey",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSecretKey",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeWebhookSecret",
                schema: "platform",
                table: "TenantSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllowedPaymentGateways",
                schema: "platform",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "platform",
                table: "Tenants",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentModes",
                schema: "platform",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EasypaisaCredentials",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "EasypaisaEnabled",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "EasypaisaHashKey",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "EasypaisaStoreId",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "JazzCashEnabled",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "JazzCashHashKey",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "JazzCashMerchantId",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "JazzCashPassword",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "JazzCashReturnUrl",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ManualPaymentEnabled",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "ManualPaymentInstructions",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "StripeEnabled",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "StripePublishableKey",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "StripeSecretKey",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "StripeWebhookSecret",
                schema: "platform",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "AllowedPaymentGateways",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "platform",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnrollmentModes",
                schema: "platform",
                table: "Tenants");
        }
    }
}
