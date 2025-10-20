using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")] //Para los roles
    
    public class MembresiasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembresiasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Membresias
        public async Task<IActionResult> Index()
        {
            return View(await _context.Membresias.ToListAsync());
        }

        // GET: Membresias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membresia == null)
            {
                return NotFound();
            }

            return View(membresia);
        }

        // GET: Membresias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Membresias/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Precio")] Membresia membresia)
        {
            if (ModelState.IsValid)
            {
                _context.Add(membresia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(membresia);
        }

        // GET: Membresias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia == null)
            {
                return NotFound();
            }
            return View(membresia);
        }

        // POST: Membresias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Precio")] Membresia membresia)
        {
            if (id != membresia.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membresia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembresiaExists(membresia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(membresia);
        }

        // GET: Membresias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membresia == null)
            {
                return NotFound();
            }

            return View(membresia);
        }

        // POST: Membresias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia != null)
            {
                _context.Membresias.Remove(membresia);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MembresiaExists(int id)
        {
            return _context.Membresias.Any(e => e.Id == id);
        }

        //Listado de Membresia por pdf
        public async Task<IActionResult> ListadoPDF()
        {
            var membresias = await _context.Membresias
                .OrderBy(m => m.Nombre)
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
                        {
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);
                        }

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Membresías")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // ============= CONTENIDO =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);   // ID
                            columns.RelativeColumn(2);    // Nombre
                            columns.RelativeColumn(1);    // Precio
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("ID").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Precio").Bold();
                        });

                        // Filas
                        foreach (var m in membresias)
                        {
                            table.Cell().Padding(5).Text(m.Id.ToString());
                            table.Cell().Padding(5).Text(m.Nombre);
                            table.Cell().Padding(5).Text($"${m.Precio:F2}");
                        }
                    });

                    // ============= PIE DE PÁGINA =============
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generado el ").FontSize(10);
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold();
                    });
                });
            })
            .GeneratePdf();

            // Abrir PDF directamente en el navegador
            Response.Headers.Add("Content-Disposition", "inline; filename=ListadoMembresias.pdf");
            return File(pdfBytes, "application/pdf");

        }
    }

}
