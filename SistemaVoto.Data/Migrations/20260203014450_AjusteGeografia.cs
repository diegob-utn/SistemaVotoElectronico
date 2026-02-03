using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVoto.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjusteGeografia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Elecciones");

            migrationBuilder.DropColumn(
                name: "Partido",
                table: "Candidatos");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "EleccionUbicaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PartidoPolitico",
                table: "Candidatos",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "EleccionUbicaciones");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "Elecciones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "PartidoPolitico",
                table: "Candidatos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Partido",
                table: "Candidatos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
