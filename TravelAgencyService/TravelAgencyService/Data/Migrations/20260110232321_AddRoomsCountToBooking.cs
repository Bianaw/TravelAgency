using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsCountToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomsCount",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoomsCount",
                table: "Bookings");
        }
    }
}
