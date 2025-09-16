using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentBookingssToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentBookings",
                table: "Schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TourID",
                table: "Bookings",
                column: "TourID");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Tours_TourID",
                table: "Bookings",
                column: "TourID",
                principalTable: "Tours",
                principalColumn: "TourID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Tours_TourID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TourID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CurrentBookings",
                table: "Schedules");
        }
    }
}
