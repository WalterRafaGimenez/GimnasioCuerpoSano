using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        // POST: REGISTRAR ASISTENCIA POR DNI
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

            var miembro = await _context.Miembros.FirstOrDefaultAsync(m => m.DNI == dni);

            if (miembro == null)
            {
                TempData["Error"] = "No se encontró ningún miembro con ese DNI.";
                return View();
            }

            bool membresiaActiva = miembro.FechaVencimiento >= DateTime.Now;
            string estadoMembresia = membresiaActiva ? "Activa" : "Inactiva";

            if (!membresiaActiva)
            {
                TempData["Error"] = $"La membresía de {miembro.Nombre} {miembro.Apellido} no está activa.";
                return View();
            }

            bool yaAsistioHoy = await _context.Asistencias
                .AnyAsync(a => a.ID_Miembro == miembro.Id && a.FechaHora.Date == DateTime.Today);

            if (yaAsistioHoy)
            {
                TempData["Error"] = $"El miembro {miembro.Nombre} {miembro.Apellido} ya registró asistencia hoy.";
                return View();
            }

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

        // -----------------------------
        // LISTADO DE MIEMBROS PARA REGISTRAR ASISTENCIA
        // -----------------------------
        public async Task<IActionResult> Registrar()
        {
            var hoy = DateTime.Today;
            var mañana = hoy.AddDays(1);

            var miembrosActivos = await _context.Miembros
                .Where(m => m.FechaVencimiento >= DateTime.Now)
                .OrderBy(m => m.Apellido)
                .ToListAsync();

            var asistenciasHoy = await _context.Asistencias
                .Where(a => a.FechaHora >= hoy && a.FechaHora < mañana)
                .ToListAsync();

            ViewBag.AsistenciasHoy = asistenciasHoy;

            return View(miembrosActivos);
        }


        // -----------------------------
        // REGISTRAR ENTRADA
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> RegistrarEntrada(int id)
        {
            var miembro = await _context.Miembros.FindAsync(id);
            if (miembro == null) return NotFound();

            bool yaAsistioHoy = await _context.Asistencias
                .AnyAsync(a => a.ID_Miembro == id && a.FechaHora.Date == DateTime.Today);

            if (yaAsistioHoy)
            {
                TempData["Error"] = $"El miembro {miembro.Nombre} {miembro.Apellido} ya registró asistencia hoy.";
                return RedirectToAction(nameof(Registrar));
            }

            var asistencia = new Asistencia
            {
                ID_Miembro = miembro.Id,
                FechaHora = DateTime.Now,
                EstadoMembresia = "Activa"
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Entrada registrada para {miembro.Nombre} {miembro.Apellido}.";
            return RedirectToAction(nameof(Registrar));
        }

        // -----------------------------
        // REGISTRAR SALIDA
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            var miembro = await _context.Miembros.FindAsync(id);
            if (miembro == null) return NotFound();

            var asistencia = await _context.Asistencias
                .Where(a => a.ID_Miembro == id && a.FechaHora.Date == DateTime.Today)
                .FirstOrDefaultAsync();

            if (asistencia == null)
            {
                TempData["Error"] = $"El miembro {miembro.Nombre} {miembro.Apellido} no tiene una asistencia registrada hoy.";
                return RedirectToAction(nameof(Registrar));
            }

            if (asistencia.HoraSalida != null)
            {
                TempData["Error"] = $"El miembro {miembro.Nombre} {miembro.Apellido} ya registró su salida hoy.";
                return RedirectToAction(nameof(Registrar));
            }

            asistencia.HoraSalida = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Salida registrada correctamente para {miembro.Nombre} {miembro.Apellido}.";
            return RedirectToAction(nameof(Registrar));
        }

        // IMPRIMIR POR PDF LISTA DE ASISTENCIAS
        public async Task<IActionResult> ImprimirListado()
        {
            var asistencias = await _context.Asistencias
                .Include(a => a.Miembro)
                .OrderByDescending(a => a.FechaHora)
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

                    // ===== CABECERA =====
                    page.Header().Column(header =>
                    {
                        if (logoBytes != null)
                        {
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);
                        }

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Asistencias")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // ===== CONTENIDO =====
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Nombre
                            columns.RelativeColumn(2); // Apellido
                            columns.RelativeColumn(2); // DNI
                            columns.RelativeColumn(2); // Fecha
                            columns.RelativeColumn(2); // Hora Entrada
                            columns.RelativeColumn(2); // Hora Salida
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Apellido").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DNI").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fecha").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Entrada").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hora Salida").Bold();
                        });

                        // Filas
                        foreach (var a in asistencias)
                        {
                            table.Cell().Padding(5).Text(a.Miembro?.Nombre ?? "");
                            table.Cell().Padding(5).Text(a.Miembro?.Apellido ?? "");
                            table.Cell().Padding(5).Text(a.Miembro?.DNI ?? "");
                            table.Cell().Padding(5).Text(a.FechaHora.ToString("dd/MM/yyyy"));
                            table.Cell().Padding(5).Text(a.FechaHora.ToString("HH:mm"));
                            table.Cell().Padding(5).Text(a.HoraSalida?.ToString("HH:mm") ?? "-");
                        }
                    });

                    // ===== PIE DE PÁGINA =====
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generado el ").FontSize(10);
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold();
                    });
                });
            })
            .GeneratePdf();

            // Abrir PDF directamente en el navegador
            Response.Headers.Add("Content-Disposition", "inline; filename=ListadoAsistencias.pdf");
            return File(pdfBytes, "application/pdf");
        }

    }
}


