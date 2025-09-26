using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaxUsageAndAddIsDeletedToVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUsage",
                table: "Vouchers");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vouchers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_IsDeleted",
                table: "Vouchers",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_IsDeleted",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vouchers");

            migrationBuilder.AddColumn<int>(
                name: "MaxUsage",
                table: "Vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
