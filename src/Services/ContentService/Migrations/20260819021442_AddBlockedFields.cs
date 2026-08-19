using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "block_reason",
                table: "video",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "blocked_at",
                table: "video",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "block_reason",
                table: "video");

            migrationBuilder.DropColumn(
                name: "blocked_at",
                table: "video");
        }
    }
}
