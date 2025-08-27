using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImageDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Images",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
