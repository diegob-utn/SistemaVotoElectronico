using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaVoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class v2_ubicaciones_opcionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones");

            migrationBuilder.AddColumn<int>(
                name: "RecintoId",
                table: "Votos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionId",
                table: "Votos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecintoId",
                table: "HistorialVotaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionId",
                table: "HistorialVotaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModoUbicacion",
                table: "Elecciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UsaUbicacion",
                table: "Elecciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ubicaciones_Ubicaciones_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EleccionUbicaciones",
                columns: table => new
                {
                    EleccionId = table.Column<int>(type: "integer", nullable: false),
                    UbicacionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EleccionUbicaciones", x => new { x.EleccionId, x.UbicacionId });
                    table.ForeignKey(
                        name: "FK_EleccionUbicaciones_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EleccionUbicaciones_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recintos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UbicacionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recintos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recintos_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_RecintoId",
                table: "Votos",
                column: "RecintoId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_UbicacionId",
                table: "Votos",
                column: "UbicacionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos",
                sql: "(\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_RecintoId",
                table: "HistorialVotaciones",
                column: "RecintoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_UbicacionId",
                table: "HistorialVotaciones",
                column: "UbicacionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones",
                sql: "(\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_EleccionUbicaciones_UbicacionId",
                table: "EleccionUbicaciones",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Recintos_UbicacionId_Nombre",
                table: "Recintos",
                columns: new[] { "UbicacionId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_ParentId_Tipo_Nombre",
                table: "Ubicaciones",
                columns: new[] { "ParentId", "Tipo", "Nombre" });

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialVotaciones_Recintos_RecintoId",
                table: "HistorialVotaciones",
                column: "RecintoId",
                principalTable: "Recintos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialVotaciones_Ubicaciones_UbicacionId",
                table: "HistorialVotaciones",
                column: "UbicacionId",
                principalTable: "Ubicaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Votos_Recintos_RecintoId",
                table: "Votos",
                column: "RecintoId",
                principalTable: "Recintos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Votos_Ubicaciones_UbicacionId",
                table: "Votos",
                column: "UbicacionId",
                principalTable: "Ubicaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialVotaciones_Recintos_RecintoId",
                table: "HistorialVotaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialVotaciones_Ubicaciones_UbicacionId",
                table: "HistorialVotaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Votos_Recintos_RecintoId",
                table: "Votos");

            migrationBuilder.DropForeignKey(
                name: "FK_Votos_Ubicaciones_UbicacionId",
                table: "Votos");

            migrationBuilder.DropTable(
                name: "EleccionUbicaciones");

            migrationBuilder.DropTable(
                name: "Recintos");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropIndex(
                name: "IX_Votos_RecintoId",
                table: "Votos");

            migrationBuilder.DropIndex(
                name: "IX_Votos_UbicacionId",
                table: "Votos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialVotaciones_RecintoId",
                table: "HistorialVotaciones");

            migrationBuilder.DropIndex(
                name: "IX_HistorialVotaciones_UbicacionId",
                table: "HistorialVotaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones");

            migrationBuilder.DropColumn(
                name: "RecintoId",
                table: "Votos");

            migrationBuilder.DropColumn(
                name: "UbicacionId",
                table: "Votos");

            migrationBuilder.DropColumn(
                name: "RecintoId",
                table: "HistorialVotaciones");

            migrationBuilder.DropColumn(
                name: "UbicacionId",
                table: "HistorialVotaciones");

            migrationBuilder.DropColumn(
                name: "ModoUbicacion",
                table: "Elecciones");

            migrationBuilder.DropColumn(
                name: "UsaUbicacion",
                table: "Elecciones");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voto_ExactamenteUno",
                table: "Votos",
                sql: "((\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Eleccion_EscanosSegunTipo",
                table: "Elecciones",
                sql: "((\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0))");
        }
    }
}
