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
            // 1️⃣ Obtener el DNI del miembro logueado desde los claims
            // Obtener el miembro logueado de forma segura
            var dniClaim = User.Claims.FirstOrDefault(c => c.Type == "DNI")?.Value;
            var email = User.Identity?.Name; // normalmente es el mail de login

            Miembro? miembro = null;

            if (!string.IsNullOrEmpty(dniClaim))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dniClaim);
            else if (!string.IsNullOrEmpty(email))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null)
                return Unauthorized("Miembro no encontrado en la base de datos.");

            // 3️⃣ Verificar que el horario exista
            var horario = await _context.HorariosClase.FirstOrDefaultAsync(h => h.Id == horarioClaseId);
            if (horario == null)
                return NotFound("El horario de clase no existe.");

            // 4️⃣ Verificar si ya está inscripto
            var existe = await _context.InscripcionClase
                .AnyAsync(i => i.HorarioClaseId == horarioClaseId && i.MiembroId == miembro.Id);

            if (!existe)
            {
                var inscripcion = new InscripcionClase
                {
                    HorarioClaseId = horarioClaseId,
                    MiembroId = miembro.Id
                };
                _context.InscripcionClase.Add(inscripcion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "ClasesMiembro");
        }

        // Acción para desinscribirse de un horario de clase
        public async Task<IActionResult> Desinscribirse(int horarioClaseId)
        {
            // Obtener el miembro logueado de forma segura
            var dniClaim = User.Claims.FirstOrDefault(c => c.Type == "DNI")?.Value;
            var email = User.Identity?.Name; // normalmente es el mail de login

            Miembro? miembro = null;

            if (!string.IsNullOrEmpty(dniClaim))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dniClaim);
            else if (!string.IsNullOrEmpty(email))
                miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null)
                return Unauthorized("Miembro no encontrado en la base de datos.");
            var inscripcion = await _context.InscripcionClase
                .FirstOrDefaultAsync(i => i.HorarioClaseId == horarioClaseId && i.MiembroId == miembro.Id);

            if (inscripcion != null)
            {
                _context.InscripcionClase.Remove(inscripcion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "ClasesMiembro");
        }
    }
}

