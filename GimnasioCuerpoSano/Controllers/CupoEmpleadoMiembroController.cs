using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GimnasioCuerpoSano.Controllers
{
    public class CupoEmpleadoMiembroController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CupoEmpleadoMiembroController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CupoEmpleadoMiembro/Disponibilidad/5
        public IActionResult Disponibilidad(int horarioClaseId)
        {
            var horarioClase = _context.HorariosClase
                                       .Include(hc => hc.Inscripciones)
                                       .FirstOrDefault(hc => hc.Id == horarioClaseId);

            if (horarioClase == null)
                return NotFound();

            // Contamos solo inscripciones activas (si tenés un estado para confirmar)
            int inscritos = horarioClase.Inscripciones.Count();
            bool hayCupo = inscritos < horarioClase.CapacidadMaxima;

            return Json(new
            {
                HorarioClaseId = horarioClase.Id,
                HayCupo = hayCupo,
                Inscritos = inscritos,
                CapacidadMaxima = horarioClase.CapacidadMaxima
            });
        }
    }
}
