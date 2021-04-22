using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class RenamePokedexIdToPokemonName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PokedexId",
                table: "SessionLogEntry");

            migrationBuilder.AddColumn<string>(
                name: "PokemonName",
                table: "SessionLogEntry",
                type: "nvarchar(24)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PokemonName",
                table: "SessionLogEntry");

            migrationBuilder.AddColumn<int>(
                name: "PokedexId",
                table: "SessionLogEntry",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
