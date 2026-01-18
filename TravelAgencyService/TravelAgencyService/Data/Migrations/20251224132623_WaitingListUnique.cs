using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class WaitingListUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaitingListEntries_TravelPackageId",
                table: "WaitingListEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "WaitingListEntries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_TravelPackageId_Email",
                table: "WaitingListEntries",
                columns: new[] { "TravelPackageId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaitingListEntries_TravelPackageId_Email",
                table: "WaitingListEntries");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "WaitingListEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_TravelPackageId",
                table: "WaitingListEntries",
                column: "TravelPackageId");
        }
    }
}
