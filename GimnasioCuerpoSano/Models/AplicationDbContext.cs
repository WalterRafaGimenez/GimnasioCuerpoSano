using GimnasioCuerpoSano.Models;
using Microsoft.EntityFrameworkCore;

namespace GimnasioCuerpoSano.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Miembro> Miembros { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
    }
}
