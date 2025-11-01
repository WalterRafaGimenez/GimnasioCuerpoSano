using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class EmpleadoController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmpleadoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Paso 1: Seleccionar miembro
    public async Task<IActionResult> SeleccionarMiembro()
    {
        var miembros = await _context.Miembros.ToListAsync();
        return View(miembros);
    }

    // Paso 2: Mostrar clases disponibles para el miembro
    public async Task<IActionResult> VerClases(int miembroId)
    {
        var miembro = await _context.Miembros.FindAsync(miembroId);
        if (miembro == null) return NotFound();

        // Cargar HorarioClases + Clase + Entrenador + Sala + Inscripciones
        var clases = await _context.HorariosClase
            .Include(h => h.Clase)
                .ThenInclude(c => c.Entrenador)
            .Include(h => h.Sala)
            .Include(h => h.Inscripciones)
            .ToListAsync();

        var modelo = new ClasesEmpleadoViewModel
        {
            MiembroId = miembro.Id,
            NombreCompletoMiembro = $"{miembro.Nombre} {miembro.Apellido}",
            ClasesDisponibles = clases
        };

        return View(modelo);
    }

    // Paso 3: Inscribir miembro en clase
    [HttpPost]
    public async Task<IActionResult> Inscribirse(int miembroId, int horarioClaseId)
    {
        var existe = await _context.InscripcionClase
            .AnyAsync(i => i.MiembroId == miembroId && i.HorarioClaseId == horarioClaseId);

        if (!existe)
        {
            _context.InscripcionClase.Add(new InscripcionClase
            {
                MiembroId = miembroId,
                HorarioClaseId = horarioClaseId,
                Estado = true
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("VerClases", new { miembroId });
    }

    // Paso 4: Desinscribir miembro de clase
    [HttpPost]
    public async Task<IActionResult> Desinscribirse(int miembroId, int horarioClaseId)
    {
        var inscripcion = await _context.InscripcionClase
            .FirstOrDefaultAsync(i => i.MiembroId == miembroId && i.HorarioClaseId == horarioClaseId);

        if (inscripcion != null)
        {
            _context.InscripcionClase.Remove(inscripcion);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("VerClases", new { miembroId });
    }
}

