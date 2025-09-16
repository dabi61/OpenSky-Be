using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTourModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfDays",
                table: "Tours");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Tours",
                newName: "TourName");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tours",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Tours",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Tours",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Transportation",
                table: "Tours",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Transportation",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tours");

            migrationBuilder.RenameColumn(
                name: "TourName",
                table: "Tours",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tours",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfDays",
                table: "Tours",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
