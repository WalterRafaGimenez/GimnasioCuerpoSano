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

            //Evita el borrado en cascada en la relación Cobro / Miembro y Cobro / Membresía
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
        }

        public DbSet<Miembro> Miembros { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Cobro> Cobros { get; set; }

    }
}
