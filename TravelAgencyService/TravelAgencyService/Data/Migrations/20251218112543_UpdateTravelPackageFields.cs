using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgencyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTravelPackageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "TravelPackages");

            migrationBuilder.RenameColumn(
                name: "PopularityCount",
                table: "TravelPackages",
                newName: "PackageType");

            migrationBuilder.RenameColumn(
                name: "MinimumAge",
                table: "TravelPackages",
                newName: "AgeLimit");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TravelPackages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Images",
                table: "TravelPackages");

            migrationBuilder.RenameColumn(
                name: "PackageType",
                table: "TravelPackages",
                newName: "PopularityCount");

            migrationBuilder.RenameColumn(
                name: "AgeLimit",
                table: "TravelPackages",
                newName: "MinimumAge");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "TravelPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
