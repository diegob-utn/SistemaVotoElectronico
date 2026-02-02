using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVoto.Data.Migrations
{
    public partial class v2_ajustada : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Modifica restricciones si es necesario
            migrationBuilder.DropCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones");

            // ...aquí puedes modificar o agregar nuevas restricciones, índices o relaciones...

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos",
                sql: "(\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones",
                sql: "(\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones");

            // Restaura las restricciones originales si es necesario
            migrationBuilder.AddCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos",
                sql: "(\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones",
                sql: "(\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0)");
        }
    }
}