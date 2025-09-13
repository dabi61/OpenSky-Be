using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHotelCoordinatesToLatLon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_BookingID",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Coordinates",
                table: "Hotels");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Hotels",
                type: "numeric(18,15)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Hotels",
                type: "numeric(18,15)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BookingID",
                table: "Bills",
                column: "BookingID",
                unique: true,
                filter: "\"BookingID\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bills_BookingID",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Hotels");

            migrationBuilder.AddColumn<string>(
                name: "Coordinates",
                table: "Hotels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BookingID",
                table: "Bills",
                column: "BookingID",
                unique: true);
        }
    }
}
