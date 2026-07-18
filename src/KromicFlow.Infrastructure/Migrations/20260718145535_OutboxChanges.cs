using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KromicFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Sessions",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "InstagramAccounts",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Automations",
                newName: "xmin");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Sessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "InstagramAccounts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresUtc",
                table: "InstagramAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Automations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "DeadLetterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    FailedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetterEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RefreshTokenHash",
                table: "Sessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterEvents_FailedUtc",
                table: "DeadLetterEvents",
                column: "FailedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_CreatedUtc",
                table: "OutboxEvents",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedUtc",
                table: "OutboxEvents",
                column: "ProcessedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeadLetterEvents");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_RefreshTokenHash",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TokenExpiresUtc",
                table: "InstagramAccounts");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Sessions",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "InstagramAccounts",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Automations",
                newName: "Version");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Sessions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "InstagramAccounts",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Automations",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }
    }
}
