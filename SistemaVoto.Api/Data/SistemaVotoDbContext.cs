using Microsoft.EntityFrameworkCore;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Data
{
    public class SistemaVotoDbContext : DbContext
    {
        public SistemaVotoDbContext(DbContextOptions<SistemaVotoDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Eleccion> Elecciones => Set<Eleccion>();
        public DbSet<Lista> Listas => Set<Lista>();
        public DbSet<Candidato> Candidatos => Set<Candidato>();
        public DbSet<Voto> Votos => Set<Voto>();
        public DbSet<HistorialVotacion> HistorialVotaciones => Set<HistorialVotacion>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // =========================
            // UNIQUE / INTEGRIDAD
            // =========================
            mb.Entity<Rol>()
              .HasIndex(r => r.Nombre)
              .IsUnique();

            mb.Entity<Usuario>()
              .HasIndex(u => u.Email)
              .IsUnique();

            mb.Entity<HistorialVotacion>()
              .HasIndex(h => new { h.EleccionId, h.UsuarioId })
              .IsUnique();

            // =========================
            // RELACIONES
            // =========================
            mb.Entity<Lista>()
              .HasOne(l => l.Eleccion)
              .WithMany(e => e.Listas)
              .HasForeignKey(l => l.EleccionId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Candidato>()
              .HasOne(c => c.Eleccion)
              .WithMany(e => e.Candidatos)
              .HasForeignKey(c => c.EleccionId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Candidato>()
              .HasOne(c => c.Lista)
              .WithMany(l => l.Candidatos)
              .HasForeignKey(c => c.ListaId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<Voto>()
              .HasOne(v => v.Eleccion)
              .WithMany(e => e.Votos)
              .HasForeignKey(v => v.EleccionId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<Voto>()
              .HasOne(v => v.Candidato)
              .WithMany()
              .HasForeignKey(v => v.CandidatoId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Voto>()
              .HasOne(v => v.Lista)
              .WithMany()
              .HasForeignKey(v => v.ListaId)
              .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // CHECK CONSTRAINTS (POSTGRES)
            // =========================

            // 1) Voto: EXACTAMENTE UNO (CandidatoId XOR ListaId)
            mb.Entity<Voto>()
              .ToTable(t => t.HasCheckConstraint(
                "CK_Voto_ExactamenteUno",
                "((\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL))"
              ));

            // 2) Elección: coherencia escaños vs tipo
            // TipoEleccion.Nominal = 0  => NumEscanos = 0
            // TipoEleccion.Plancha = 1  => NumEscanos > 0
            mb.Entity<Eleccion>()
              .ToTable(t => t.HasCheckConstraint(
                "CK_Eleccion_EscanosSegunTipo",
                "((\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0))"
              ));

            // =========================
            // ÍNDICES
            // =========================
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.CandidatoId });
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.ListaId });
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.FechaVotoUtc });

            mb.Entity<Lista>().HasIndex(l => new { l.EleccionId, l.Nombre });
            mb.Entity<Candidato>().HasIndex(c => new { c.EleccionId, c.Nombre });
        }
    }
}
