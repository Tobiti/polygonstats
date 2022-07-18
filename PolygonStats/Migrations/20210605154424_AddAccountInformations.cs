using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class AddAccountInformations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalXp",
                table: "Account",
                newName: "TotalGainedXp");

            migrationBuilder.RenameColumn(
                name: "TotalStardust",
                table: "Account",
                newName: "TotalGainedStardust");

            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pokecoins",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stardust",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Pokecoins",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Stardust",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "TotalGainedXp",
                table: "Account",
                newName: "TotalXp");

            migrationBuilder.RenameColumn(
                name: "TotalGainedStardust",
                table: "Account",
                newName: "TotalStardust");
        }
    }
}
