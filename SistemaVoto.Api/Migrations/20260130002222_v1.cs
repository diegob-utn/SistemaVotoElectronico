using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaVoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Elecciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    NumEscanos = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    UsaUbicacion = table.Column<bool>(type: "boolean", nullable: false),
                    ModoUbicacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elecciones", x => x.Id);
                    table.CheckConstraint("CK_Eleccion_EscanosSegunTipo", "(\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0)");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

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
                name: "Listas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    EleccionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listas_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "Candidatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PartidoPolitico = table.Column<string>(type: "text", nullable: true),
                    FotoUrl = table.Column<string>(type: "text", nullable: true),
                    Propuestas = table.Column<string>(type: "text", nullable: true),
                    EleccionId = table.Column<int>(type: "integer", nullable: false),
                    ListaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidatos_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Candidatos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HistorialVotaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EleccionId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    FechaParticipacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HashTransaccion = table.Column<string>(type: "text", nullable: true),
                    UbicacionId = table.Column<int>(type: "integer", nullable: true),
                    RecintoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialVotaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialVotaciones_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialVotaciones_Recintos_RecintoId",
                        column: x => x.RecintoId,
                        principalTable: "Recintos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistorialVotaciones_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistorialVotaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EleccionId = table.Column<int>(type: "integer", nullable: false),
                    CandidatoId = table.Column<int>(type: "integer", nullable: true),
                    ListaId = table.Column<int>(type: "integer", nullable: true),
                    FechaVotoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HashPrevio = table.Column<string>(type: "text", nullable: false),
                    HashActual = table.Column<string>(type: "text", nullable: false),
                    UbicacionId = table.Column<int>(type: "integer", nullable: true),
                    RecintoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votos", x => x.Id);
                    table.CheckConstraint("CK_Voto_ExactamenteUno", "(\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Votos_Candidatos_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votos_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votos_Recintos_RecintoId",
                        column: x => x.RecintoId,
                        principalTable: "Recintos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Votos_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_EleccionId_Nombre",
                table: "Candidatos",
                columns: new[] { "EleccionId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_ListaId",
                table: "Candidatos",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_EleccionUbicaciones_UbicacionId",
                table: "EleccionUbicaciones",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_EleccionId_UsuarioId",
                table: "HistorialVotaciones",
                columns: new[] { "EleccionId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_RecintoId",
                table: "HistorialVotaciones",
                column: "RecintoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_UbicacionId",
                table: "HistorialVotaciones",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVotaciones_UsuarioId",
                table: "HistorialVotaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Listas_EleccionId_Nombre",
                table: "Listas",
                columns: new[] { "EleccionId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Recintos_UbicacionId_Nombre",
                table: "Recintos",
                columns: new[] { "UbicacionId", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_ParentId_Tipo_Nombre",
                table: "Ubicaciones",
                columns: new[] { "ParentId", "Tipo", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_CandidatoId",
                table: "Votos",
                column: "CandidatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_EleccionId_CandidatoId",
                table: "Votos",
                columns: new[] { "EleccionId", "CandidatoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_EleccionId_FechaVotoUtc",
                table: "Votos",
                columns: new[] { "EleccionId", "FechaVotoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_EleccionId_ListaId",
                table: "Votos",
                columns: new[] { "EleccionId", "ListaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Votos_ListaId",
                table: "Votos",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_RecintoId",
                table: "Votos",
                column: "RecintoId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_UbicacionId",
                table: "Votos",
                column: "UbicacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EleccionUbicaciones");

            migrationBuilder.DropTable(
                name: "HistorialVotaciones");

            migrationBuilder.DropTable(
                name: "Votos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Candidatos");

            migrationBuilder.DropTable(
                name: "Recintos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Listas");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropTable(
                name: "Elecciones");
        }
    }
}
