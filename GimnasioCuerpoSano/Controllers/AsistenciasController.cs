using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GimnasioCuerpoSano.Controllers
{
    public class AsistenciasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AsistenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -----------------------------
        // LISTADO DE ASISTENCIAS
        // -----------------------------
        public async Task<IActionResult> Index()
        {
            var asistencias = await _context.Asistencias
                .Include(a => a.Miembro)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();

            return View(asistencias);
        }

        // -----------------------------
        // FORMULARIO PARA CREAR ASISTENCIA
        // -----------------------------
        public IActionResult Create()
        {
            return View();
        }

        // -----------------------------
        // POST: REGISTRAR ASISTENCIA
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                TempData["Error"] = "Debe ingresar un DNI.";
                return View();
            }

            // Buscar el miembro por DNI
            var miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dni);

            if (miembro == null)
            {
                TempData["Error"] = "No se encontró ningún miembro con ese DNI.";
                return View();
            }

            // Determinar estado de la membresía
            bool membresiaActiva = miembro.FechaVencimiento >= DateTime.Now;
            string estadoMembresia = membresiaActiva ? "Activa" : "Inactiva";

            // Si la membresía no está activa, no se permite registrar
            if (!membresiaActiva)
            {
                TempData["Error"] = $"La membresía de {miembro.Nombre} {miembro.Apellido} no está activa.";
                return View();
            }

            // Verificar si ya registró asistencia hoy
            bool yaAsistioHoy = await _context.Asistencias
                .AnyAsync(a => a.ID_Miembro == miembro.Id && a.FechaHora.Date == DateTime.Today);

            if (yaAsistioHoy)
            {
                TempData["Error"] = $"El miembro {miembro.Nombre} {miembro.Apellido} ya registró asistencia hoy.";
                return View();
            }

            // Crear registro de asistencia
            var asistencia = new Asistencia
            {
                ID_Miembro = miembro.Id,
                FechaHora = DateTime.Now,
                EstadoMembresia = estadoMembresia
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Asistencia registrada para {miembro.Nombre} {miembro.Apellido} a las {asistencia.FechaHora:HH:mm}";

            return RedirectToAction(nameof(Index));
        }
    }
}

