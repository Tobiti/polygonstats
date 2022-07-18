using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class MorePokemonInformations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Costume",
                table: "SessionLogEntry",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Form",
                table: "SessionLogEntry",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Shadow",
                table: "SessionLogEntry",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Costume",
                table: "SessionLogEntry");

            migrationBuilder.DropColumn(
                name: "Form",
                table: "SessionLogEntry");

            migrationBuilder.DropColumn(
                name: "Shadow",
                table: "SessionLogEntry");
        }
    }
}
