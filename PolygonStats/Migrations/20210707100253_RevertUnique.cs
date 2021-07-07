using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class RevertUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogEntry_PokemonUniqueId",
                table: "SessionLogEntry");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntry_PokemonUniqueId",
                table: "SessionLogEntry",
                column: "PokemonUniqueId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogEntry_PokemonUniqueId",
                table: "SessionLogEntry");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntry_PokemonUniqueId",
                table: "SessionLogEntry",
                column: "PokemonUniqueId",
                unique: true);
        }
    }
}
