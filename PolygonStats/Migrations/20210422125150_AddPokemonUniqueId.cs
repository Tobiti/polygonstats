using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class AddPokemonUniqueId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attack",
                table: "SessionLogEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Defense",
                table: "SessionLogEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<ulong>(
                name: "PokemonUniqueId",
                table: "SessionLogEntry",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<int>(
                name: "Stamina",
                table: "SessionLogEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attack",
                table: "SessionLogEntry");

            migrationBuilder.DropColumn(
                name: "Defense",
                table: "SessionLogEntry");

            migrationBuilder.DropColumn(
                name: "PokemonUniqueId",
                table: "SessionLogEntry");

            migrationBuilder.DropColumn(
                name: "Stamina",
                table: "SessionLogEntry");
        }
    }
}
