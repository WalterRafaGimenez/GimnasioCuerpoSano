using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // Necesario para el Enum

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

            // ----------------------------------------------------
            // SOLUCIÓN 1: Mapeo del Enum a String (Arregla InvalidCastException)
            // ----------------------------------------------------
            modelBuilder.Entity<HorarioClase>()
                .Property(h => h.DiaSemana)
                // Usamos EnumToStringConverter para que EF Core lea y escriba el nombre del enum 
                // ("Lunes", "Martes") en la columna nvarchar.
                .HasConversion(new EnumToStringConverter<DiaSemana>());

            // -----------------------------
            // Evita el borrado en cascada para Cobros
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
                .HasPrecision(18, 2);

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

            // -----------------------------
            // Configuración de decimales para Clase
            // -----------------------------
            modelBuilder.Entity<Clase>()
                .Property(c => c.Precio)
                .HasPrecision(18, 2); // O la precisión que necesites, (18 dígitos en total, 2 después del punto)

            // -----------------------------
            // ❌ SOLUCIÓN 2: Relaciones de Clase (Eliminamos mapeos redundantes que causaban el conflicto SalaId)
            // -----------------------------
            modelBuilder.Entity<Clase>()
                .ToTable("Clase"); // Mantenemos la convención de nombre de tabla si es necesaria


            // -----------------------------
            // Relación Clase → Horarios (Mantenemos, ya que es la relación "padre-hijo" con CASCADE)
            // -----------------------------
            modelBuilder.Entity<Clase>()
                .HasMany(c => c.Horarios)
                .WithOne(h => h.Clase)
                .HasForeignKey(h => h.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------------
            // Relaciones HorarioClase e InscripcionClase
            // -----------------------------
            modelBuilder.Entity<HorarioClase>()
                .ToTable("HorarioClase")
                .HasMany(h => h.Inscripciones)
                .WithOne(i => i.HorarioClase)
                .HasForeignKey(i => i.HorarioClaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InscripcionClase>()
                .HasOne(i => i.Miembro)
                .WithMany()
                .HasForeignKey(i => i.MiembroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asistencia>().ToTable("Asistencia");
        }


        // -----------------------------
        // DbSets
        // -----------------------------
        public DbSet<Miembro> Miembros { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Cobro> Cobros { get; set; }
        public DbSet<Entrenador> Entrenadores { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<Clase> Clase { get; set; }
        public DbSet<HorarioClase> HorariosClase { get; set; }
        public DbSet<InscripcionClase> InscripcionClase { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
    }
}