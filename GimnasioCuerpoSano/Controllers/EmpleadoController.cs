using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    public async Task<IActionResult> ImprimirInscripciones()
    {
        // Traemos solo los miembros que tienen al menos una inscripción
        var miembros = await _context.Miembros
            .Include(m => m.Inscripciones)
                .ThenInclude(i => i.HorarioClase)
                    .ThenInclude(h => h.Clase)
                        .ThenInclude(c => c.Entrenador)
            .Where(m => m.Inscripciones.Any(i => i.Estado)) // solo inscripciones activas
            .OrderBy(m => m.Apellido)
            .ThenBy(m => m.Nombre)
            .ToListAsync();

        // Ruta del logo
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

                // ============= CABECERA =============
                page.Header().Column(header =>
                {
                    if (logoBytes != null)
                        header.Item().AlignCenter().Container().Width(100).Image(logoBytes);

                    header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Medium).AlignCenter();

                    header.Item().PaddingTop(5).Text("Listado de Inscripciones a Clases")
                        .FontSize(16).Bold().FontColor(Colors.Grey.Darken1).AlignCenter();
                });

                // ============= CONTENIDO =============
                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Nombre
                        columns.RelativeColumn(2); // Apellido
                        columns.RelativeColumn(3); // Clase
                        columns.RelativeColumn(3); // Entrenador
                        columns.RelativeColumn(2); // Hora inicio
                        columns.RelativeColumn(2); // Hora fin
                    });

                    // Encabezado
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Apellido").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Clase").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Entrenador").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Inicio").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Fin").Bold();
                    });

                    // Filas
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
        })
        .GeneratePdf();

        // Abrir PDF directamente en el navegador
        Response.Headers.Add("Content-Disposition", "inline; filename=InscripcionesClases.pdf");
        return File(pdfBytes, "application/pdf");
    }


}

