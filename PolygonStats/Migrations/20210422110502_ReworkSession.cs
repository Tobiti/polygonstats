using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PolygonStats.Migrations
{
    public partial class ReworkSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Egg");

            migrationBuilder.DropTable(
                name: "Fort");

            migrationBuilder.DropTable(
                name: "Pokemon");

            migrationBuilder.DropTable(
                name: "Quest");

            migrationBuilder.CreateTable(
                name: "SessionLogEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    LogEntryType = table.Column<string>(type: "nvarchar(24)", nullable: false),
                    CaughtSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PokedexId = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    XpReward = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StardustReward = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Shiny = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntry_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntry_SessionId",
                table: "SessionLogEntry",
                column: "SessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogEntry");

            migrationBuilder.CreateTable(
                name: "Egg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    StardustReward = table.Column<int>(type: "int", nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Egg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Egg_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fort",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fort", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fort_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pokemon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PokedexId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Shiny = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StardustReward = table.Column<int>(type: "int", nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pokemon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pokemon_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    StardustReward = table.Column<int>(type: "int", nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quest_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Egg_SessionId",
                table: "Egg",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Fort_SessionId",
                table: "Fort",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_SessionId",
                table: "Pokemon",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Quest_SessionId",
                table: "Quest",
                column: "SessionId");
        }
    }
}
