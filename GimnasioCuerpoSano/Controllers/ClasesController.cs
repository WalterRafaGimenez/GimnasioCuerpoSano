using System;
using System.IO;
using System.Linq;
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
            var clases = _context.Clase
                .Include(c => c.Entrenador)
                .Include(c => c.Sala)
                .OrderBy(c => c.Nombre);

            return View(await clases.ToListAsync());
        }

        // =====================================================
        // DETAILS
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var clase = await _context.Clase
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
            ViewBag.Entrenadores = new SelectList(_context.Entrenadores.OrderBy(e => e.Apellido), "Id", "Apellido");
            ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Numero");
            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Clase clase)
        {
            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.ID_Sala == clase.SalaId);

            // 🔹 Validaciones personalizadas
            if (sala != null && clase.CupoMaximo > sala.CapacidadMaxima)
            {
                ModelState.AddModelError("CupoMaximo", $"El cupo máximo no puede ser mayor que la capacidad de la sala (máximo {sala.CapacidadMaxima}).");
            }

            if (clase.CupoMaximo <= 0)
            {
                ModelState.AddModelError("CupoMaximo", "El cupo máximo debe ser mayor que cero.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Entrenadores = new SelectList(_context.Entrenadores, "Id", "Apellido", clase.EntrenadorId);
                ViewBag.Salas = new SelectList(_context.Salas, "ID_Sala", "Numero", clase.SalaId);
                return View(clase);
            }

            _context.Clase.Add(clase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        public async Task<IActionResult> Edit(int id)
        {
            var clase = await _context.Clase.FindAsync(id);
            if (clase == null) return NotFound();

            ViewBag.Entrenadores = new SelectList(_context.Entrenadores, "Id", "Apellido", clase.EntrenadorId);
            ViewBag.Salas = new SelectList(_context.Salas, "ID_Sala", "Numero", clase.SalaId);
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

            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.ID_Sala == clase.SalaId);

            // 🔹 Validaciones personalizadas
            if (sala != null && clase.CupoMaximo > sala.CapacidadMaxima)
            {
                ModelState.AddModelError("CupoMaximo", $"El cupo máximo no puede ser mayor que la capacidad de la sala (máximo {sala.CapacidadMaxima}).");
            }

            if (clase.CupoMaximo <= 0)
            {
                ModelState.AddModelError("CupoMaximo", "El cupo máximo debe ser mayor que cero.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Entrenadores = new SelectList(_context.Entrenadores, "Id", "Apellido", clase.EntrenadorId);
                ViewBag.Salas = new SelectList(_context.Salas, "ID_Sala", "Numero", clase.SalaId);
                return View(clase);
            }

            try
            {
                _context.Update(clase);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clase.Any(c => c.Id == id))
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
            var clase = await _context.Clase
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
            var clase = await _context.Clase.FindAsync(id);
            if (clase != null)
            {
                _context.Clase.Remove(clase);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GENERAR PDF LISTADO DE CLASES
        // =====================================================
        public async Task<IActionResult> GenerarListadoPdf()
        {
            var clases = await _context.Clase
                .Include(c => c.Entrenador)
                .Include(c => c.Sala)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Logo.png");
            byte[]? logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;

            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.PageColor(Colors.White);

                    // CABECERA
                    page.Header().Column(header =>
                    {
                        if (logoBytes != null)
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Clases")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // CONTENIDO
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);   // Nombre
                            columns.ConstantColumn(80);  // Precio
                            columns.ConstantColumn(80);  // Duración
                            columns.RelativeColumn(2);   // Entrenador
                            columns.RelativeColumn(1);   // Sala
                        });

                        // Encabezado
                        table.Header(headerRow =>
                        {
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Precio").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Duración (min)").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Entrenador").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Sala").Bold();
                        });

                        // Filas
                        foreach (var c in clases)
                        {
                            table.Cell().Padding(5).Text(c.Nombre);
                            table.Cell().Padding(5).Text(c.Precio.ToString("C2"));
                            table.Cell().Padding(5).Text(c.DuracionMinutos.ToString());
                            table.Cell().Padding(5).Text(c.Entrenador != null ? $"{c.Entrenador.Apellido}, {c.Entrenador.Nombre}" : "");
                            table.Cell().Padding(5).Text(c.Sala != null ? c.Sala.Numero : "");
                        }
                    });

                    // PIE DE PÁGINA
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Line("© Gimnasio Cuerpo Sano " + DateTime.Now.Year);
                        x.Line("Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            })
            .GeneratePdf();

            Response.Headers["Content-Disposition"] = "inline; filename=Listado_Clases.pdf";
            return File(pdfBytes, "application/pdf");
        }
    }
}
