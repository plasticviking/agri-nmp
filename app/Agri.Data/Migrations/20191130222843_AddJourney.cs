﻿using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Agri.Data.Migrations
{
    public partial class AddJourney : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JourneyId",
                table: "SubMenu",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "JourneyId",
                table: "MainMenus",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Journeys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubMenu_JourneyId",
                table: "SubMenu",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_MainMenus_JourneyId",
                table: "MainMenus",
                column: "JourneyId");

            migrationBuilder.AddForeignKey(
                name: "FK_MainMenus_Journeys_JourneyId",
                table: "MainMenus",
                column: "JourneyId",
                principalTable: "Journeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubMenu_Journeys_JourneyId",
                table: "SubMenu",
                column: "JourneyId",
                principalTable: "Journeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MainMenus_Journeys_JourneyId",
                table: "MainMenus");

            migrationBuilder.DropForeignKey(
                name: "FK_SubMenu_Journeys_JourneyId",
                table: "SubMenu");

            migrationBuilder.DropTable(
                name: "Journeys");

            migrationBuilder.DropIndex(
                name: "IX_SubMenu_JourneyId",
                table: "SubMenu");

            migrationBuilder.DropIndex(
                name: "IX_MainMenus_JourneyId",
                table: "MainMenus");

            migrationBuilder.DropColumn(
                name: "JourneyId",
                table: "SubMenu");

            migrationBuilder.DropColumn(
                name: "JourneyId",
                table: "MainMenus");
        }
    }
}
