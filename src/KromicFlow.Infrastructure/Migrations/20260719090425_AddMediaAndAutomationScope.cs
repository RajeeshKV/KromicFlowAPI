using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAndAutomationScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Automations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InstagramMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstagramAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstagramMediaId = table.Column<string>(type: "text", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Caption = table.Column<string>(type: "character varying(2200)", maxLength: 2200, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Permalink = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstagramMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstagramMedia_InstagramAccounts_InstagramAccountId",
                        column: x => x.InstagramAccountId,
                        principalTable: "InstagramAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutomationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstagramMediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationMedia_Automations_AutomationId",
                        column: x => x.AutomationId,
                        principalTable: "Automations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutomationMedia_InstagramMedia_InstagramMediaId",
                        column: x => x.InstagramMediaId,
                        principalTable: "InstagramMedia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Automations_Scope",
                table: "Automations",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationMedia_AutomationId",
                table: "AutomationMedia",
                column: "AutomationId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationMedia_AutomationId_InstagramMediaId",
                table: "AutomationMedia",
                columns: new[] { "AutomationId", "InstagramMediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationMedia_InstagramMediaId",
                table: "AutomationMedia",
                column: "InstagramMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramMedia_InstagramAccountId",
                table: "InstagramMedia",
                column: "InstagramAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramMedia_InstagramMediaId",
                table: "InstagramMedia",
                column: "InstagramMediaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstagramMedia_MediaType",
                table: "InstagramMedia",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramMedia_PostedAtUtc",
                table: "InstagramMedia",
                column: "PostedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationMedia");

            migrationBuilder.DropTable(
                name: "InstagramMedia");

            migrationBuilder.DropIndex(
                name: "IX_Automations_Scope",
                table: "Automations");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Automations");
        }
    }
}
