using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GimnasioCuerpoSano.Controllers
{
    public class ClasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var clases = _context.Clases
                .Include(c => c.Entrenador)
                .Include(c => c.Sala);
            return View(await clases.ToListAsync());
        }

        // =====================================================
        // DETAILS
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var clase = await _context.Clases
                .Include(c => c.Entrenador)
                .Include(c => c.Sala)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clase == null) return NotFound();
            return View(clase);
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================
        public IActionResult Create()
        {
            ViewBag.Entrenadores = _context.Entrenadores.ToList();
            ViewBag.Salas = _context.Salas.ToList();
            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Clase clase)
        {
            if (clase.Precio <= 0 || clase.DuracionMinutos <= 0)
            {
                ModelState.AddModelError("", "El precio y la duración deben ser mayores a cero.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Entrenadores = _context.Entrenadores.ToList();
                ViewBag.Salas = _context.Salas.ToList();
                return View(clase);
            }

            _context.Clases.Add(clase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        public async Task<IActionResult> Edit(int id)
        {
            var clase = await _context.Clases.FindAsync(id);
            if (clase == null) return NotFound();

            ViewBag.Entrenadores = _context.Entrenadores.ToList();
            ViewBag.Salas = _context.Salas.ToList();

            return View(clase);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Clase clase)
        {
            if (id != clase.Id) return NotFound();

            if (clase.Precio <= 0 || clase.DuracionMinutos <= 0)
            {
                ModelState.AddModelError("", "El precio y la duración deben ser mayores a cero.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Entrenadores = _context.Entrenadores.ToList();
                ViewBag.Salas = _context.Salas.ToList();
                return View(clase);
            }

            try
            {
                _context.Update(clase);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clases.Any(c => c.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE (GET)
        // =====================================================
        public async Task<IActionResult> Delete(int id)
        {
            var clase = await _context.Clases
                .Include(c => c.Entrenador)
                .Include(c => c.Sala)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clase == null) return NotFound();
            return View(clase);
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clase = await _context.Clases.FirstOrDefaultAsync(c => c.Id == id);
            if (clase != null)
            {
                _context.Clases.Remove(clase);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GENERAR PDF LISTADO DE CLASES ESTILIZADO - ABRIR EN NAVEGADOR
        // =====================================================
        public async Task<IActionResult> GenerarListadoPdf()
        {
            var clases = await _context.Clases
                .Include(c => c.Entrenador)
                .Include(c => c.Sala)
                .OrderBy(c => c.Nombre)
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

                    // ============= CABECERA ESTILIZADA =============
                    page.Header().Column(header =>
                    {
                        if (logoBytes != null)
                            header.Item().AlignCenter().Container().Width(120).Image(logoBytes);

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(22).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(2).Text("Listado de Clases")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();

                        header.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    // ============= CONTENIDO CON TABLA ESTILIZADA =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        // Columnas
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);   // Nombre
                            columns.ConstantColumn(80);  // Precio
                            columns.ConstantColumn(80);  // Duración
                            columns.RelativeColumn(2);   // Entrenador
                            columns.RelativeColumn(2);   // Sala
                        });

                        // Encabezado
                        table.Header(headerRow =>
                        {
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Nombre").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Precio").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Duración (min)").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Entrenador").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(6).Text("Sala").Bold();
                        });

                        // Filas alternadas para mejor legibilidad
                        bool alternar = false;
                        foreach (var c in clases)
                        {
                            var bgColor = alternar ? Colors.Grey.Lighten4 : Colors.White;
                            table.Cell().Background(bgColor).Padding(5).Text(c.Nombre);
                            table.Cell().Background(bgColor).Padding(5).Text("$" + c.Precio.ToString("N2"));
                            table.Cell().Background(bgColor).Padding(5).Text(c.DuracionMinutos.ToString());
                            table.Cell().Background(bgColor).Padding(5).Text(c.Entrenador != null ? $"{c.Entrenador.Apellido}, {c.Entrenador.Nombre}" : "");
                            table.Cell().Background(bgColor).Padding(5).Text(c.Sala != null ? c.Sala.Nombre : "");
                            alternar = !alternar;
                        }
                    });

                    // ============= PIE DE PÁGINA ESTILIZADO =============
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Line("© Gimnasio Cuerpo Sano " + DateTime.Now.Year);
                        x.Line("Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            })
            .GeneratePdf();

            // Abrir PDF directamente en nueva pestaña
            Response.Headers["Content-Disposition"] = "inline; filename=Listado_Clases.pdf";
            return File(pdfBytes, "application/pdf");
        }

    }
}
