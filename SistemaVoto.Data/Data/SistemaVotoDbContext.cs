using Microsoft.EntityFrameworkCore;
using SistemaVoto.Modelos;

namespace SistemaVoto.Data.Data
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

        //  NUEVO
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<RecintoElectoral> Recintos => Set<RecintoElectoral>();
        public DbSet<EleccionUbicacion> EleccionUbicaciones => Set<EleccionUbicacion>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // -------------------------
            // Unique
            // -------------------------
            mb.Entity<Rol>()
              .HasIndex(r => r.Nombre)
              .IsUnique();

            mb.Entity<Usuario>()
              .HasIndex(u => u.Email)
              .IsUnique();

            mb.Entity<HistorialVotacion>()
              .HasIndex(h => new { h.EleccionId, h.UsuarioId })
              .IsUnique();

            // -------------------------
            // Relaciones core
            // -------------------------
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

            // -------------------------
            // Ubicaciones (opcionales)
            // -------------------------
            mb.Entity<Ubicacion>()
              .HasOne(u => u.Parent)
              .WithMany(u => u.Children)
              .HasForeignKey(u => u.ParentId)
              .OnDelete(DeleteBehavior.Restrict);

            mb.Entity<RecintoElectoral>()
              .HasOne(r => r.Ubicacion)
              .WithMany()
              .HasForeignKey(r => r.UbicacionId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<Voto>()
              .HasOne(v => v.Ubicacion)
              .WithMany()
              .HasForeignKey(v => v.UbicacionId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<Voto>()
              .HasOne(v => v.Recinto)
              .WithMany()
              .HasForeignKey(v => v.RecintoId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<HistorialVotacion>()
              .HasOne(h => h.Ubicacion)
              .WithMany()
              .HasForeignKey(h => h.UbicacionId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<HistorialVotacion>()
              .HasOne(h => h.Recinto)
              .WithMany()
              .HasForeignKey(h => h.RecintoId)
              .OnDelete(DeleteBehavior.SetNull);

            mb.Entity<EleccionUbicacion>()
              .HasKey(x => new { x.EleccionId, x.UbicacionId });

            mb.Entity<EleccionUbicacion>()
              .HasOne(x => x.Eleccion)
              .WithMany(e => e.EleccionUbicaciones)
              .HasForeignKey(x => x.EleccionId)
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<EleccionUbicacion>()
              .HasOne(x => x.Ubicacion)
              .WithMany()
              .HasForeignKey(x => x.UbicacionId)
              .OnDelete(DeleteBehavior.Cascade);

            // -------------------------
            //  CHECK constraints (Postgres) CORRECTOS
            // -------------------------
            mb.Entity<Voto>().ToTable(t => t.HasCheckConstraint(
                "CK_Voto_ExactamenteUno",
                "(\"CandidatoId\" IS NOT NULL AND \"ListaId\" IS NULL) OR (\"CandidatoId\" IS NULL AND \"ListaId\" IS NOT NULL)"
            ));

            mb.Entity<Eleccion>().ToTable(t => t.HasCheckConstraint(
                "CK_Eleccion_EscanosSegunTipo",
                "(\"Tipo\" = 0 AND \"NumEscanos\" = 0) OR (\"Tipo\" = 1 AND \"NumEscanos\" > 0)"
            ));

            // -------------------------
            // Índices
            // -------------------------
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.CandidatoId });
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.ListaId });
            mb.Entity<Voto>().HasIndex(v => new { v.EleccionId, v.FechaVotoUtc });

            mb.Entity<Lista>().HasIndex(l => new { l.EleccionId, l.Nombre });
            mb.Entity<Candidato>().HasIndex(c => new { c.EleccionId, c.Nombre });

            mb.Entity<Ubicacion>().HasIndex(u => new { u.ParentId, u.Tipo, u.Nombre });
            mb.Entity<RecintoElectoral>().HasIndex(r => new { r.UbicacionId, r.Nombre });
        }
    }
}
