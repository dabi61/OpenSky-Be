using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTableIDFromVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "TableID",
                table: "Vouchers");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Tours",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tours",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsReaded",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills",
                column: "UserVoucherID",
                principalTable: "UserVouchers",
                principalColumn: "UserVoucherID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills");

            migrationBuilder.AddColumn<Guid>(
                name: "TableID",
                table: "Vouchers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Tours",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tours",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsReaded",
                table: "Messages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills",
                column: "UserVoucherID",
                principalTable: "UserVouchers",
                principalColumn: "UserVoucherID");
        }
    }
}
