using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAndItineraryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tours_TourID",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Users_UserID",
                table: "Schedules");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TourItineraries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TourID1",
                table: "TourItineraries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Schedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TourID1",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserID1",
                table: "Schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraries_DayNumber",
                table: "TourItineraries",
                column: "DayNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraries_IsDeleted",
                table: "TourItineraries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TourItineraries_TourID1",
                table: "TourItineraries",
                column: "TourID1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_CreatedAt",
                table: "Schedules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_Status",
                table: "Schedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TourID1",
                table: "Schedules",
                column: "TourID1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_UserID1",
                table: "Schedules",
                column: "UserID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tours_TourID",
                table: "Schedules",
                column: "TourID",
                principalTable: "Tours",
                principalColumn: "TourID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tours_TourID1",
                table: "Schedules",
                column: "TourID1",
                principalTable: "Tours",
                principalColumn: "TourID");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Users_UserID",
                table: "Schedules",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Users_UserID1",
                table: "Schedules",
                column: "UserID1",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_TourItineraries_Tours_TourID1",
                table: "TourItineraries",
                column: "TourID1",
                principalTable: "Tours",
                principalColumn: "TourID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tours_TourID",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Tours_TourID1",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Users_UserID",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Users_UserID1",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_TourItineraries_Tours_TourID1",
                table: "TourItineraries");

            migrationBuilder.DropIndex(
                name: "IX_TourItineraries_DayNumber",
                table: "TourItineraries");

            migrationBuilder.DropIndex(
                name: "IX_TourItineraries_IsDeleted",
                table: "TourItineraries");

            migrationBuilder.DropIndex(
                name: "IX_TourItineraries_TourID1",
                table: "TourItineraries");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_CreatedAt",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_Status",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TourID1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_UserID1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TourItineraries");

            migrationBuilder.DropColumn(
                name: "TourID1",
                table: "TourItineraries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TourID1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "UserID1",
                table: "Schedules");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Tours_TourID",
                table: "Schedules",
                column: "TourID",
                principalTable: "Tours",
                principalColumn: "TourID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Users_UserID",
                table: "Schedules",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
