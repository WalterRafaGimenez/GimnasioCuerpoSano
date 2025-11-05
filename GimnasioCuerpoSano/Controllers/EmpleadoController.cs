using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    public async Task<IActionResult> VerClases(int? miembroId)
    {
        if (miembroId == null)
        {
            TempData["Error"] = "Debe seleccionar un miembro.";
            return RedirectToAction("SeleccionarMiembro");
        }

        var miembro = await _context.Miembros.FindAsync(miembroId);
        if (miembro == null)
        {
            TempData["Error"] = "El miembro seleccionado no existe.";
            return RedirectToAction("SeleccionarMiembro");
        }

        // Cargar HorarioClases + Clase + Entrenador + Sala + Inscripciones
        var clases = await _context.HorariosClase
            .Include(h => h.Clase)
                .ThenInclude(c => c.Entrenador)
            .Include(h => h.Sala)
            .Include(h => h.Inscripciones)
            .ToListAsync();

        // Convertimos a ViewModel y calculamos si está lleno
        var clasesViewModel = clases.Select(h =>
        {
            int inscriptosActivos = h.Inscripciones.Count(i => i.Estado);
            int cupoMaximo = h.Clase?.CupoMaximo ?? 0;

            return new InscripcionClaseViewModel
            {
                Id = h.Id,
                NombreClase = h.Clase?.Nombre,
                NombreEntrenador = h.Clase?.Entrenador != null
                    ? h.Clase.Entrenador.Nombre + " " + h.Clase.Entrenador.Apellido
                    : "Sin entrenador",
                HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                EstaLleno = cupoMaximo > 0 && inscriptosActivos >= cupoMaximo,
                Estado = h.Inscripciones.Any(i => i.MiembroId == miembroId && i.Estado)
                         ? "Inscripto"
                         : "No Inscripto"
            };
        }).ToList();

        var modelo = new ClasesEmpleadoViewModel
        {
            MiembroId = miembro.Id,
            NombreCompletoMiembro = $"{miembro.Nombre} {miembro.Apellido}",
            ClasesDisponibles = clasesViewModel
        };

        return View(modelo);
    }


    // Paso 3: Inscribir miembro en clase (con validación de cupo)
    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> Inscribirse(int miembroId, int horarioClaseId)
    {
        var miembro = await _context.Miembros.FindAsync(miembroId);
        if (miembro == null) return NotFound();

        var horario = await _context.HorariosClase
            .Include(h => h.Clase)
            .Include(h => h.Inscripciones)
            .FirstOrDefaultAsync(h => h.Id == horarioClaseId);

        if (horario == null)
        {
            TempData["Error"] = "No se encontró la clase seleccionada.";
            return RedirectToAction("VerClases", new { miembroId });
        }

        // Validar si ya está inscripto
        bool yaInscripto = horario.Inscripciones?.Any(i => i.MiembroId == miembroId && i.Estado) ?? false;
        if (yaInscripto)
        {
            TempData["Error"] = "El miembro ya está inscripto en esta clase.";
            return RedirectToAction("VerClases", new { miembroId });
        }

        // Validar cupo usando la capacidad del horario
        int inscriptosActivos = horario.Inscripciones?.Count(i => i.Estado) ?? 0;
        int cupo = horario.CapacidadMaxima; // <-- Capacidad específica del horario
        if (cupo > 0 && inscriptosActivos >= cupo)
        {
            TempData["Error"] = "No hay cupos disponibles para este horario.";
            return RedirectToAction("VerClases", new { miembroId });
        }

        // Crear inscripción
        _context.InscripcionClase.Add(new InscripcionClase
        {
            MiembroId = miembroId,
            HorarioClaseId = horarioClaseId,
            Estado = true,
            FechaInscripcion = System.DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Inscripto correctamente.";
        return RedirectToAction("VerClases", new { miembroId });
    }

    // Paso 4: Desinscribir miembro de clase
    [HttpPost]
    public async Task<IActionResult> Desinscribirse(int miembroId, int horarioClaseId)
    {
        var inscripcion = await _context.InscripcionClase
            .FirstOrDefaultAsync(i => i.MiembroId == miembroId && i.HorarioClaseId == horarioClaseId && i.Estado);

        if (inscripcion != null)
        {
            _context.InscripcionClase.Remove(inscripcion);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Inscripción cancelada.";
        }

        return RedirectToAction("VerClases", new { miembroId });
    }

    // Paso 5: Generar PDF de inscripciones (igual que antes)
    public async Task<IActionResult> ImprimirInscripciones()
    {
        var miembros = await _context.Miembros
            .Include(m => m.Inscripciones)
                .ThenInclude(i => i.HorarioClase)
                    .ThenInclude(h => h.Clase)
                        .ThenInclude(c => c.Entrenador)
            .Where(m => m.Inscripciones.Any(i => i.Estado))
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .ToListAsync();

        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Logo.png");
        byte[]? logoBytes = System.IO.File.Exists(logoPath)
            ? System.IO.File.ReadAllBytes(logoPath)
            : null;

        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));
                page.PageColor(Colors.White);

                page.Header().Column(header =>
                {
                    if (logoBytes != null)
                        header.Item().AlignCenter().Container().Width(100).Image(logoBytes);

                    header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Medium).AlignCenter();

                    header.Item().PaddingTop(5).Text("Listado de Inscripciones a Clases")
                        .FontSize(16).Bold().FontColor(Colors.Grey.Darken1).AlignCenter();
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Apellido").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Clase").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Entrenador").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Inicio").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Fin").Bold();
                    });

                    foreach (var m in miembros)
                    {
                        foreach (var insc in m.Inscripciones)
                        {
                            var clase = insc.HorarioClase?.Clase;
                            var entrenador = clase?.Entrenador;

                            table.Cell().Padding(5).Text(m.Nombre);
                            table.Cell().Padding(5).Text(m.Apellido);
                            table.Cell().Padding(5).Text(clase?.Nombre ?? "—");
                            table.Cell().Padding(5).Text(entrenador != null ? $"{entrenador.Nombre} {entrenador.Apellido}" : "—");
                            table.Cell().Padding(5).Text(insc.HorarioClase?.HoraInicio.ToString(@"hh\:mm") ?? "—");
                            table.Cell().Padding(5).Text(insc.HorarioClase?.HoraFin.ToString(@"hh\:mm") ?? "—");
                        }
                    }
                });
            });
        }).GeneratePdf();

        Response.Headers.Add("Content-Disposition", "inline; filename=InscripcionesClases.pdf");
        return File(pdfBytes, "application/pdf");
    }
}
