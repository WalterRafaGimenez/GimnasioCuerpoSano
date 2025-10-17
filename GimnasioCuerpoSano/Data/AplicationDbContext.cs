using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GimnasioCuerpoSano.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // Evita el borrado en cascada
            // -----------------------------
            modelBuilder.Entity<Cobro>()
                .HasOne(c => c.Miembro)
                .WithMany()
                .HasForeignKey(c => c.MiembroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cobro>()
                .HasOne(c => c.Membresia)
                .WithMany()
                .HasForeignKey(c => c.MembresiaId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------
            // Configuración de decimales
            // -----------------------------
            modelBuilder.Entity<Cobro>()
                .Property(c => c.Monto)
                .HasPrecision(18, 2); // 18 dígitos, 2 decimales

            modelBuilder.Entity<Membresia>()
                .Property(m => m.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Miembro>()
                .Property(m => m.ValorMembresia)
                .HasPrecision(18, 2);

            // -----------------------------
            // Código de barras obligatorio
            // -----------------------------
            modelBuilder.Entity<Miembro>()
                .Property(m => m.CodigoBarra)
                .IsRequired();
        }

        public DbSet<Miembro> Miembros { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Cobro> Cobros { get; set; }
        public DbSet<Entrenador> Entrenadores { get; set; }
        public DbSet<Sala> Salas { get; set; }
    }
}

