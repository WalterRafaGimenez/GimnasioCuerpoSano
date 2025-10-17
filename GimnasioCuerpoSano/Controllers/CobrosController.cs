using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    public class CobrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CobrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTAR (INDEX)
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var cobros = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .ToListAsync();

            return View(cobros);
        }

        // =====================================================
        // CREAR (GET)
        // =====================================================
        public IActionResult Create()
        {
            ViewBag.Miembros = _context.Miembros.ToList();
            ViewBag.Membresias = _context.Membresias.ToList();
            return View();
        }

        // =====================================================
        // CREAR (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cobro cobro)
        {
            if (ModelState.IsValid)
            {
                var membresia = await _context.Membresias.FindAsync(cobro.MembresiaId);
                var miembro = await _context.Miembros.FindAsync(cobro.MiembroId);

                if (membresia == null || miembro == null)
                {
                    ModelState.AddModelError("", "Debe seleccionar un miembro y una membresía válidos.");
                    ViewBag.Miembros = _context.Miembros.ToList();
                    ViewBag.Membresias = _context.Membresias.ToList();
                    return View(cobro);
                }

                cobro.Monto = membresia.Precio;
                cobro.FechaPago = DateTime.Now;
                cobro.Estado = CalcularEstado(membresia.Nombre, cobro.FechaPago);
                cobro.Codigo = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                _context.Cobros.Add(cobro);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = $"Cobro registrado correctamente. Código: {cobro.Codigo}";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Miembros = _context.Miembros.ToList();
            ViewBag.Membresias = _context.Membresias.ToList();
            return View(cobro);
        }

        // =====================================================
        // DETALLES
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var cobro = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cobro == null)
                return NotFound();

            return View(cobro);
        }

        // =====================================================
        // ELIMINAR (GET)
        // =====================================================
        public async Task<IActionResult> Delete(int id)
        {
            var cobro = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cobro == null)
                return NotFound();

            return View(cobro);
        }

        // =====================================================
        // ELIMINAR (POST)
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cobro = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cobro == null)
                return NotFound();

            try
            {
                _context.Cobros.Remove(cobro);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Cobro eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este cobro porque está relacionado con otro registro.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // FUNCIÓN AUXILIAR: ESTADO
        // =====================================================
        private string CalcularEstado(string tipoMembresia, DateTime fechaPago)
        {
            int diasVigencia = tipoMembresia switch
            {
                "Mensual" => 30,
                "Trimestral" => 90,
                "Anual" => 365,
                _ => 30
            };

            var fechaVencimiento = fechaPago.AddDays(diasVigencia);
            return DateTime.Now > fechaVencimiento ? "Vencido" : "Vigente";
        }

        // =====================================================
        // GENERAR PDF (QuestPDF) - ABRIR EN NAVEGADOR
        // =====================================================
        public async Task<IActionResult> GenerarReciboPdf(int id)
        {
            var cobro = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cobro == null)
                return NotFound();

            // Crear PDF en memoria
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header()
                        .Text("🏋️‍♂️ Gimnasio Cuerpo Sano")
                        .SemiBold().FontSize(20).AlignCenter().FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Item().Text("Comprobante de Pago").Bold().FontSize(16).AlignCenter().FontColor(Colors.Black);
                            column.Item().Text($"Código: {cobro.Codigo}");
                            column.Item().Text($"Socio: {cobro.Miembro?.Nombre} {cobro.Miembro?.Apellido}");
                            column.Item().Text($"Membresía: {cobro.Membresia?.Nombre}");
                            column.Item().Text($"Método de Pago: {cobro.MetodoPago}");
                            column.Item().Text($"Monto: ${cobro.Monto:N2}");
                            column.Item().Text($"Fecha de Pago: {cobro.FechaPago:g}");
                            column.Item().Text($"Estado: {cobro.Estado}")
                                  .FontColor(cobro.Estado == "Vencido" ? Colors.Red.Medium : Colors.Green.Medium);
                            column.Item().PaddingTop(20).Text("Gracias por su pago.").Italic().AlignCenter();
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("© Gimnasio Cuerpo Sano ");
                            x.Span(DateTime.Now.Year.ToString());
                        });
                });
            })
            .GeneratePdf();

            Response.Headers["Content-Disposition"] = $"inline; filename=Recibo_{cobro.Codigo}.pdf";
            return File(pdfBytes, "application/pdf");
        }
    }
}

