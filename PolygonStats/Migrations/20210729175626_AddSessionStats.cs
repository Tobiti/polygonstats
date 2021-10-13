using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class AddSessionStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaughtPokemon",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EscapedPokemon",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Session",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MaxIV",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pokestops",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Raids",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rockets",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Shadow",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShinyPokemon",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGainedStardust",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGainedXp",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMinutes",
                table: "Session",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaughtPokemon",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "EscapedPokemon",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "MaxIV",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "Pokestops",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "Raids",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "Rockets",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "Shadow",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "ShinyPokemon",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "TotalGainedStardust",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "TotalGainedXp",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "TotalMinutes",
                table: "Session");
        }
    }
}
