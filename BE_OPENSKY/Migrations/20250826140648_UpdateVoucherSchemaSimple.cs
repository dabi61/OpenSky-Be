using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoucherSchemaSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vouchers: đổi TableID uuid -> int
            migrationBuilder.DropColumn(
                name: "TableID",
                table: "Vouchers");

            migrationBuilder.AddColumn<int>(
                name: "TableID",
                table: "Vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Vouchers: đổi Quantity -> MaxUsage
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Vouchers");

            migrationBuilder.AddColumn<int>(
                name: "MaxUsage",
                table: "Vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // UserVouchers: thêm SavedAt
            migrationBuilder.AddColumn<DateTime>(
                name: "SavedAt",
                table: "UserVouchers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavedAt",
                table: "UserVouchers");

            migrationBuilder.DropColumn(
                name: "TableID",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MaxUsage",
                table: "Vouchers");

            migrationBuilder.AddColumn<Guid>(
                name: "TableID",
                table: "Vouchers",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
