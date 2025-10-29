using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Miembro,EmpleadoMiembro")]
    public class ClasesMiembroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClasesMiembroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClasesMiembro
        public async Task<IActionResult> Index()
        {
            // Obtener el usuario logueado
            var email = User.Identity?.Name;
            var miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null)
                return Unauthorized("Miembro no encontrado.");

            // Traer todos los horarios de clases
            var horarios = await _context.HorariosClase
                .Include(h => h.Clase)
                    .ThenInclude(c => c.Entrenador)
                .Include(h => h.Sala)
                .Include(h => h.Inscripciones)
                .ToListAsync();

            // Mapear a ViewModel
            var model = horarios.Select(h => new ClaseDisponibleViewModel
            {
                HorarioClaseId = h.Id,
                NombreClase = h.Clase.Nombre,
                SalaNombre = h.Sala.Nombre,
                DiaSemana = h.DiaSemana.ToString(),
                HoraInicio = h.HoraInicio,
                DuracionMinutos = h.Clase.DuracionMinutos,
                Precio = h.Clase.Precio,
                EntrenadorNombre = h.Clase.Entrenador.Nombre,
                EstaInscripto = h.Inscripciones.Any(i => i.MiembroId == miembro.Id)
            }).ToList();

            return View(model);
        }
    }
}
