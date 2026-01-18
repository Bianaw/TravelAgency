using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LatestBookingDaysBeforeStart = table.Column<int>(type: "int", nullable: false),
                    CancellationDaysBeforeStart = table.Column<int>(type: "int", nullable: false),
                    ReminderDaysBeforeStart = table.Column<int>(type: "int", nullable: false),
                    MaxActiveBookings = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRules");
        }
    }
}
