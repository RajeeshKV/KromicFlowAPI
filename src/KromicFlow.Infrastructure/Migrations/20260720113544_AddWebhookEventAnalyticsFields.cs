using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookEventAnalyticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AutomationId",
                table: "WebhookEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentId",
                table: "WebhookEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "WebhookEvents",
                type: "character varying(2200)",
                maxLength: 2200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommenterIgId",
                table: "WebhookEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommenterUsername",
                table: "WebhookEvents",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstagramAccountId",
                table: "WebhookEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaIgId",
                table: "WebhookEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_AutomationId",
                table: "WebhookEvents",
                column: "AutomationId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_CommenterIgId",
                table: "WebhookEvents",
                column: "CommenterIgId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_InstagramAccountId",
                table: "WebhookEvents",
                column: "InstagramAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ReceivedUtc",
                table: "WebhookEvents",
                column: "ReceivedUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookEvents_Automations_AutomationId",
                table: "WebhookEvents",
                column: "AutomationId",
                principalTable: "Automations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookEvents_InstagramAccounts_InstagramAccountId",
                table: "WebhookEvents",
                column: "InstagramAccountId",
                principalTable: "InstagramAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebhookEvents_Automations_AutomationId",
                table: "WebhookEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_WebhookEvents_InstagramAccounts_InstagramAccountId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_AutomationId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_CommenterIgId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_InstagramAccountId",
                table: "WebhookEvents");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_ReceivedUtc",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "AutomationId",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "CommenterIgId",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "CommenterUsername",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "InstagramAccountId",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "MediaIgId",
                table: "WebhookEvents");
        }
    }
}
