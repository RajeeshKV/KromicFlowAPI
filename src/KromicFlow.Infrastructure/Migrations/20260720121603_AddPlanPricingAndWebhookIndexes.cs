using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KromicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanPricingAndWebhookIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingPeriod",
                table: "Plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "monthly");

            migrationBuilder.AddColumn<int>(
                name: "PriceInrPaise",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "BillingPeriod", "MonthlyAutomationRuns", "PriceInrPaise" },
                values: new object[] { "monthly", 500, 0 });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "BillingPeriod", "Code", "ConfigurationJson", "CreatedUtc", "IsActive", "IsDefault", "MaxAutomations", "MaxInstagramAccounts", "MonthlyAutomationRuns", "MonthlyEmails", "MonthlyPushNotifications", "Name", "PriceInrPaise", "UpdatedUtc", "Version" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000002"), "monthly", "starter", "{}", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 10, 2, 2000, 100, 100, "Starter", 9900, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "monthly", "pro", "{}", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), true, false, 50, 5, 10000, 500, 500, "Pro", 29900, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_CommentId",
                table: "WebhookEvents",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_InstagramAccountId_CommenterIgId",
                table: "WebhookEvents",
                columns: new[] { "InstagramAccountId", "CommenterIgId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_CommentId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_InstagramAccountId_CommenterIgId",
                table: "WebhookEvents");

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DropColumn(
                name: "BillingPeriod",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "PriceInrPaise",
                table: "Plans");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "MonthlyAutomationRuns",
                value: 100);
        }
    }
}
