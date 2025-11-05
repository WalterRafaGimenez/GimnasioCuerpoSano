using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Miembro,EmpleadoMiembro")]
    public class InscripcionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscripcionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acción para inscribirse a un horario de clase
        public async Task<IActionResult> Inscribirse(int horarioClaseId)
        {
            // 1️⃣ Obtener el miembro logueado
            var dniClaim = User.Claims.FirstOrDefault(c => c.Type == "DNI")?.Value;
            var email = User.Identity?.Name;

            Miembro? miembro = null;

            if (!string.IsNullOrEmpty(dniClaim))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dniClaim);
            else if (!string.IsNullOrEmpty(email))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null)
                return Unauthorized("Miembro no encontrado en la base de datos.");

            // 2️⃣ Verificar que el horario exista
            var horario = await _context.HorariosClase
                .Include(h => h.Inscripciones)
                .FirstOrDefaultAsync(h => h.Id == horarioClaseId);

            if (horario == null)
                return NotFound("El horario de clase no existe.");

            // 3️⃣ Verificar si ya está inscripto
            var yaInscripto = horario.Inscripciones.Any(i => i.MiembroId == miembro.Id);

            if (yaInscripto)
            {
                TempData["Error"] = "Ya estás inscripto en esta clase.";
                return RedirectToAction("Index", "ClasesMiembro");
            }

            // 4️⃣ Verificar el cupo máximo usando HorarioClase.CapacidadMaxima
            int inscriptosActuales = horario.Inscripciones.Count;
            int cupoMaximo = horario.CapacidadMaxima;

            if (inscriptosActuales >= cupoMaximo)
            {
                TempData["Error"] = "El cupo de esta clase ya está completo.";
                return RedirectToAction("Index", "ClasesMiembro");
            }

            // 5️⃣ Crear la inscripción
            var inscripcion = new InscripcionClase
            {
                HorarioClaseId = horarioClaseId,
                MiembroId = miembro.Id
            };

            _context.InscripcionClase.Add(inscripcion);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Inscripción realizada correctamente.";
            return RedirectToAction("Index", "ClasesMiembro");
        }

        // Acción para desinscribirse de un horario de clase
        public async Task<IActionResult> Desinscribirse(int horarioClaseId)
        {
            // 1️⃣ Obtener el miembro logueado
            var dniClaim = User.Claims.FirstOrDefault(c => c.Type == "DNI")?.Value;
            var email = User.Identity?.Name;

            Miembro? miembro = null;

            if (!string.IsNullOrEmpty(dniClaim))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dniClaim);
            else if (!string.IsNullOrEmpty(email))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null)
                return Unauthorized("Miembro no encontrado en la base de datos.");

            // 2️⃣ Obtener la inscripción
            var inscripcion = await _context.InscripcionClase
                .FirstOrDefaultAsync(i => i.HorarioClaseId == horarioClaseId && i.MiembroId == miembro.Id);

            if (inscripcion == null)
            {
                TempData["Error"] = "No estás inscripto en esta clase.";
                return RedirectToAction("Index", "ClasesMiembro");
            }

            // 3️⃣ Remover inscripción y guardar cambios
            _context.InscripcionClase.Remove(inscripcion);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Te desinscribiste correctamente de la clase.";
            return RedirectToAction("Index", "ClasesMiembro");
        }

    }
}
