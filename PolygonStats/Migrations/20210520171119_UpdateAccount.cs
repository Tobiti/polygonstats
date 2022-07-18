using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class UpdateAccount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaughtPokemon",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EscapedPokemon",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pokestops",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Raids",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rockets",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShinyPokemon",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalStardust",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalXp",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaughtPokemon",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "EscapedPokemon",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Pokestops",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Raids",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "Rockets",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "ShinyPokemon",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "TotalStardust",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "TotalXp",
                table: "Account");
        }
    }
}
