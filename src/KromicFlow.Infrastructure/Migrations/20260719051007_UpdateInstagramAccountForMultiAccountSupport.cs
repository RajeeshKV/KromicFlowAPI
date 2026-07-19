using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInstagramAccountForMultiAccountSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstagramAccounts_UserId_InstagramUserId",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "ConnectedUtc",
                table: "InstagramAccounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConnectedAtUtc",
                table: "InstagramAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisconnectedAtUtc",
                table: "InstagramAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "InstagramAccounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageId",
                table: "InstagramAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsConnected",
                table: "InstagramAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTokenRefreshUtc",
                table: "InstagramAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "InstagramAccounts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenStatus",
                table: "InstagramAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramAccounts_FacebookPageId",
                table: "InstagramAccounts",
                column: "FacebookPageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstagramAccounts_InstagramUserId",
                table: "InstagramAccounts",
                column: "InstagramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstagramAccounts_UserId_InstagramUserId",
                table: "InstagramAccounts",
                columns: new[] { "UserId", "InstagramUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstagramAccounts_FacebookPageId",
                table: "InstagramAccounts");

            migrationBuilder.DropIndex(
                name: "IX_InstagramAccounts_InstagramUserId",
                table: "InstagramAccounts");

            migrationBuilder.DropIndex(
                name: "IX_InstagramAccounts_UserId_InstagramUserId",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "ConnectedAtUtc",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "DisconnectedAtUtc",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "FacebookPageId",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "IsConnected",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "LastTokenRefreshUtc",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "InstagramAccounts");

            migrationBuilder.DropColumn(
                name: "TokenStatus",
                table: "InstagramAccounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConnectedUtc",
                table: "InstagramAccounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_InstagramAccounts_UserId_InstagramUserId",
                table: "InstagramAccounts",
                columns: new[] { "UserId", "InstagramUserId" },
                unique: true);
        }
    }
}
