using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AIQXCoreService.Migrations
{
    public partial class UseCasePropRenamingAndImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_core__use_cases_core__plants_PlantId", "core__use_cases");
            migrationBuilder.DropPrimaryKey("PK_core__plants", "core__plants");

            migrationBuilder.RenameColumn(
                name: "Hall",
                table: "core__use_cases",
                newName: "Building");

            migrationBuilder.RenameColumn(
                name: "Band",
                table: "core__use_cases",
                newName: "Line");

            migrationBuilder.AlterColumn<string>(
                name: "PlantId",
                table: "core__use_cases",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "core__use_cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "core__use_cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "core__plants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");


            migrationBuilder.AddPrimaryKey("PK_core__plants", "core__plants", "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_core__use_cases_core__plants_PlantId",
                column: "PlantId",
                table: "core__use_cases",
                principalTable: "core__plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "core__use_cases");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "core__use_cases");

            migrationBuilder.RenameColumn(
                name: "Building",
                table: "core__use_cases",
                newName: "Hall");

            migrationBuilder.RenameColumn(
                name: "Line",
                table: "core__use_cases",
                newName: "Band");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlantId",
                table: "core__use_cases",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "core__plants",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
