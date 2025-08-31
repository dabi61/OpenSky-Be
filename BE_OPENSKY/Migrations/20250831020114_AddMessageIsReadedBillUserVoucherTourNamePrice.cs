using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageIsReadedBillUserVoucherTourNamePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Tours",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Tours",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsReaded",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UserVoucherID",
                table: "Bills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_UserVoucherID",
                table: "Bills",
                column: "UserVoucherID");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills",
                column: "UserVoucherID",
                principalTable: "UserVouchers",
                principalColumn: "UserVoucherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_UserVouchers_UserVoucherID",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_UserVoucherID",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsReaded",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserVoucherID",
                table: "Bills");
        }
    }
}
