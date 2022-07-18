using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class DefineIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
