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
            // 1. Agregar columna 'Activo' (boolean)
            // Se establece defaultValue: false inicialmente para registros existentes,
            // pero puedes cambiarlo a true si prefieres.
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 2. Agregar columna 'Descripcion' (texto, opcional/nullable)
            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Roles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios: eliminar las columnas
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Roles");
        }
    }
}