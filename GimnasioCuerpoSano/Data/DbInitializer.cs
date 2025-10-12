using GimnasioCuerpoSano.Models;

namespace GimnasioCuerpoSano.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Si ya hay membresías cargadas, no hacemos nada
            if (context.Membresias.Any())
            {
                return; // Ya existen, no duplicamos
            }

            var membresias = new Membresia[]
            {
                new Membresia { Nombre = "Mensual", Precio = 10000 },
                new Membresia { Nombre = "Trimestral", Precio = 27000 },
                new Membresia { Nombre = "Anual", Precio = 100000 }
            };

            context.Membresias.AddRange(membresias);
            context.SaveChanges();
        }
    }
}
