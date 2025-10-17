using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class MiembrosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MiembrosController> _logger;

        public MiembrosController(ApplicationDbContext context, ILogger<MiembrosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // LISTAR (INDEX)
        // =====================================================
        public IActionResult Index()
        {
            var miembros = _context.Miembros
                                   .Include(m => m.Membresia)
                                   .OrderBy(m => m.Apellido)
                                   .ToList();
            return View(miembros);
        }

        // =====================================================
        // DETALLES
        // =====================================================
        public IActionResult Details(int id)
        {
            var miembro = _context.Miembros
                                  .Include(m => m.Membresia)
                                  .FirstOrDefault(m => m.Id == id);

            if (miembro == null)
                return NotFound();

            return View(miembro);
        }

        // =====================================================
        // CREAR (GET)
        // =====================================================
        public IActionResult Create()
        {
            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
            return View();
        }

        // =====================================================
        // CREAR (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Miembro miembro, IFormFile? fotoArchivo)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine("Create POST iniciado: " + DateTime.Now.ToString("o"));

            try
            {
                // --------------- Validación ModelState ---------------
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState inválido al entrar al Create POST.");
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"Error en {key}: {error.ErrorMessage}");
                        }
                    }

                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                    return View(miembro); // esto muestra la misma vista: puede parecer 'pegada'
                }

                Console.WriteLine("ModelState válido. Buscando membresía...");
                var membresia = await _context.Membresias.FindAsync(miembro.MembresiaId);
                if (membresia == null)
                {
                    ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                    Console.WriteLine("Membresía no encontrada. Saliendo.");
                    return View(miembro);
                }

                // --------------- Calcula valor y fecha ---------------
                decimal valorBase = membresia.Precio;
                if (miembro.DescuentoEspecial)
                    valorBase *= 0.85m;
                miembro.ValorMembresia = valorBase;
                miembro.FechaAlta = DateTime.Now;

                // --------------- Manejo de foto (opcional) ---------------
                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    Console.WriteLine("Procesando foto...");
                    var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos");
                    if (!Directory.Exists(rutaCarpeta))
                        Directory.CreateDirectory(rutaCarpeta);

                    var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(fotoArchivo.FileName);
                    var rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);

                    using var stream = new FileStream(rutaArchivo, FileMode.Create);
                    await fotoArchivo.CopyToAsync(stream);

                    miembro.Foto = "/fotos/" + nombreArchivo;
                    Console.WriteLine("Foto guardada en: " + miembro.Foto);
                }

                // --------------- Generar Código de barras y validar unicidad ---------------
                // Intentamos generar y comprobar hasta N veces si hay colisión
                const int MAX_INTENTOS = 5;
                bool codigoUnico = false;
                string codigoGenerado = null;

                for (int intento = 0; intento < MAX_INTENTOS; intento++)
                {
                    codigoGenerado = GenerarCodigoBarra();
                    Console.WriteLine($"Intento {intento + 1}: codigo generado = {codigoGenerado}");

                    // Comprobar si ya existe en la DB
                    var existe = await _context.Miembros.AnyAsync(m => m.CodigoBarra == codigoGenerado);
                    if (!existe)
                    {
                        codigoUnico = true;
                        break;
                    }
                    Console.WriteLine("Colisión de CodigoBarra detectada. Generando otro...");
                }

                if (!codigoUnico)
                {
                    // Falla segura si no conseguimos un código único
                    ModelState.AddModelError("", "No se pudo generar un código de barras único. Intente nuevamente.");
                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                    Console.WriteLine("No se logró generar código de barras único tras varios intentos.");
                    return View(miembro);
                }

                miembro.CodigoBarra = codigoGenerado;
                Console.WriteLine("Código de barras final asignado: " + miembro.CodigoBarra);

                // --------------- Agregar y guardar ---------------
                Console.WriteLine("Antes de _context.Miembros.Add");
                _context.Miembros.Add(miembro);
                Console.WriteLine("Después de Add, antes de SaveChangesAsync");

                // Opcional: establecer un timeout corto para detectar bloqueos largos (requires SQL client config normally).
                // Aquí medimos tiempo con Stopwatch y si excede X ms, lo reportamos en consola.
                sw.Restart();
                await _context.SaveChangesAsync();
                sw.Stop();
                Console.WriteLine($"SaveChangesAsync completado en {sw.ElapsedMilliseconds} ms");

                TempData["UltimoMiembroId"] = miembro.Id;
                TempData["Mensaje"] = $"Miembro registrado correctamente. Código de barra: {miembro.CodigoBarra}";
                Console.WriteLine("Create POST finalizado con éxito. Id miembro: " + miembro.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepción al crear miembro: " + ex.ToString());
                // Agregamos mensaje de error para ver qué pasó
                ModelState.AddModelError("", "Error al guardar el miembro: " + ex.Message);
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                return View(miembro);
            }
        }



        // =====================================================
        // EDITAR (GET)
        // =====================================================
        public IActionResult Edit(int id)
        {
            var miembro = _context.Miembros.Find(id);
            if (miembro == null)
                return NotFound();

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
            return View(miembro);
        }

        // =====================================================
        // EDITAR (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Miembro miembro, IFormFile? fotoArchivo)
        {
            if (id != miembro.Id)
                return NotFound();

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);

            if (!ModelState.IsValid)
                return View(miembro);

            var miembroExistente = await _context.Miembros.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (miembroExistente == null)
                return NotFound();

            var membresia = await _context.Membresias.FindAsync(miembro.MembresiaId);
            if (membresia == null)
            {
                ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                return View(miembro);
            }

            decimal valorBase = membresia.Precio;
            if (miembro.DescuentoEspecial)
                valorBase *= 0.85m;

            miembro.ValorMembresia = valorBase;

            // FOTO
            if (fotoArchivo != null && fotoArchivo.Length > 0)
            {
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos");
                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(fotoArchivo.FileName);
                var rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);

                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await fotoArchivo.CopyToAsync(stream);
                }

                miembro.Foto = "/fotos/" + nombreArchivo;
            }
            else
            {
                miembro.Foto = miembroExistente.Foto;
            }

            miembro.CodigoBarra = miembroExistente.CodigoBarra ?? Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

            try
            {
                _context.Update(miembro);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Miembro '{miembro.Nombre} {miembro.Apellido}' actualizado correctamente. Valor: ${miembro.ValorMembresia:N2}";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Miembros.Any(e => e.Id == miembro.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ELIMINAR (GET)
        // =====================================================
        public IActionResult Delete(int id)
        {
            var miembro = _context.Miembros
                                  .Include(m => m.Membresia)
                                  .FirstOrDefault(m => m.Id == id);

            if (miembro == null)
                return NotFound();

            return View(miembro);
        }

        // =====================================================
        // ELIMINAR (POST)
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var miembro = _context.Miembros.Find(id);
            if (miembro != null)
            {
                _context.Miembros.Remove(miembro);
                _context.SaveChanges();
                TempData["Mensaje"] = "Miembro eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GENERAR PDF
        // =====================================================
        public async Task<IActionResult> GenerarPDF(int id)
        {
            var miembro = _context.Miembros.Include(m => m.Membresia).FirstOrDefault(m => m.Id == id);
            if (miembro == null)
                return NotFound();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                var title = new Paragraph("Carnet de Miembro")
                                .SetFont(boldFont)
                                .SetFontSize(18)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                document.Add(title);

                if (!string.IsNullOrEmpty(miembro.Foto))
                {
                    var fotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", miembro.Foto.TrimStart('/'));
                    if (System.IO.File.Exists(fotoPath))
                    {
                        var image = new Image(ImageDataFactory.Create(fotoPath))
                                    .ScaleToFit(150, 150)
                                    .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                        document.Add(image);
                    }
                }

                document.Add(new Paragraph($"Nombre: {miembro.Nombre}"));
                document.Add(new Paragraph($"Apellido: {miembro.Apellido}"));
                document.Add(new Paragraph($"Membresía: {miembro.Membresia?.Nombre}"));

                if (!string.IsNullOrEmpty(miembro.CodigoBarra))
                {
                    var barcodeUrl = $"https://barcode.tec-it.com/barcode.ashx?data={miembro.CodigoBarra}&code=Code128&translate-esc=true";
                    using var httpClient = new HttpClient();
                    var barcodeData = await httpClient.GetByteArrayAsync(barcodeUrl);
                    var barcodeImage = new Image(ImageDataFactory.Create(barcodeData))
                                       .ScaleToFit(200, 50)
                                       .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                    document.Add(barcodeImage);
                }

                document.Close();

                return File(ms.ToArray(), "application/pdf", $"Carnet_{miembro.Nombre}_{miembro.Apellido}.pdf");
            }
        }

        private string GenerarCodigoBarra()
        {
            // Genera un código basado en fecha y un número aleatorio
            // Formato: YYYYMMDDHHMMSS + 3 dígitos aleatorios
            var fecha = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random();
            var aleatorio = random.Next(100, 999);
            return $"{fecha}{aleatorio}";
        }



    }
}
