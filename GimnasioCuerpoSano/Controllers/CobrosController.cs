using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
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
            var cobro = await _context.Cobros.FindAsync(id);
            if (cobro != null)
            {
                _context.Cobros.Remove(cobro);
                await _context.SaveChangesAsync();
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
        // GENERAR PDF (iText7) - ABRIR EN NAVEGADOR
        // =====================================================
        public async Task<IActionResult> GenerarReciboPdf(int id)
        {
            var cobro = await _context.Cobros
                .Include(c => c.Miembro)
                .Include(c => c.Membresia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cobro == null)
                return NotFound();

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph("🏋️‍♂️ Gimnasio Cuerpo Sano")
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Comprobante de Pago")
                .SetFont(regularFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            document.Add(new Paragraph($"Código: {cobro.Codigo}").SetFont(regularFont));
            document.Add(new Paragraph($"Socio: {cobro.Miembro?.Nombre} {cobro.Miembro?.Apellido}").SetFont(regularFont));
            document.Add(new Paragraph($"Membresía: {cobro.Membresia?.Nombre}").SetFont(regularFont));
            document.Add(new Paragraph($"Método de Pago: {cobro.MetodoPago}").SetFont(regularFont));
            document.Add(new Paragraph($"Monto: ${cobro.Monto:N2}").SetFont(regularFont));
            document.Add(new Paragraph($"Fecha de Pago: {cobro.FechaPago:g}").SetFont(regularFont));
            document.Add(new Paragraph($"Estado: {cobro.Estado}")
                .SetFont(regularFont)
                .SetFontColor(cobro.Estado == "Vencido" ? ColorConstants.RED : ColorConstants.GREEN));

            document.Add(new Paragraph("\nGracias por su pago.")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFont(regularFont)
                .SetFontSize(10));

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            Response.Headers["Content-Disposition"] = $"inline; filename=Recibo_{cobro.Codigo}.pdf";
            return File(pdfBytes, "application/pdf");
        }
    }
}

