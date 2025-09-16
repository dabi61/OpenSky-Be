using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_OPENSKY.Migrations
{
    /// <inheritdoc />
    public partial class RenameDoBToDob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DoB",
                table: "Users",
                newName: "dob");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "dob",
                table: "Users",
                newName: "DoB");
        }
    }
}
