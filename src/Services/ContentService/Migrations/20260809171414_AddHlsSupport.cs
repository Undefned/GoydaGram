using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Migrations
{
    /// <inheritdoc />
    public partial class AddHlsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hls_playlist_url",
                table: "video",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "video",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hls_playlist_url",
                table: "video");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "video");
        }
    }
}
