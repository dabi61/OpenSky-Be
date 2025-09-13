using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class BookingBill_OneToOne_ByBookingID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Bills_BillID",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_HotelRooms_RoomID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BillID",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingType",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BillID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RoomID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ScheduleID",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TableType",
                table: "Bills");

            migrationBuilder.RenameColumn(
                name: "TypeID",
                table: "Bills",
                newName: "BookingID");

            migrationBuilder.AddColumn<Guid>(
                name: "RoomID",
                table: "BillDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleID",
                table: "BillDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BookingID",
                table: "Bills",
                column: "BookingID",
                unique: true,
                filter: "\"BookingID\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillDetails_RoomID_ScheduleID",
                table: "BillDetails",
                columns: new[] { "RoomID", "ScheduleID" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Bookings_BookingID",
                table: "Bills",
                column: "BookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Bookings_BookingID",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_BookingID",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_BillDetails_RoomID_ScheduleID",
                table: "BillDetails");

            migrationBuilder.DropColumn(
                name: "RoomID",
                table: "BillDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleID",
                table: "BillDetails");

            migrationBuilder.RenameColumn(
                name: "BookingID",
                table: "Bills",
                newName: "TypeID");

            migrationBuilder.AddColumn<Guid>(
                name: "BillID",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingType",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RoomID",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleID",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TableType",
                table: "Bills",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BillID",
                table: "Bookings",
                column: "BillID");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingType",
                table: "Bookings",
                column: "BookingType");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomID",
                table: "Bookings",
                column: "RoomID");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Bills_BillID",
                table: "Bookings",
                column: "BillID",
                principalTable: "Bills",
                principalColumn: "BillID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_HotelRooms_RoomID",
                table: "Bookings",
                column: "RoomID",
                principalTable: "HotelRooms",
                principalColumn: "RoomID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
