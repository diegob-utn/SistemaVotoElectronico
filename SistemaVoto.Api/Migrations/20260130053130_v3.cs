using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuario",
                table: "Usuarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Roles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Elecciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "Elecciones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Partido",
                table: "Candidatos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NombreUsuario",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Elecciones");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Elecciones");

            migrationBuilder.DropColumn(
                name: "Partido",
                table: "Candidatos");
        }
    }
}
