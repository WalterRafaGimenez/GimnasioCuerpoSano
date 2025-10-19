using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;
using ZXing.ImageSharp.Rendering;
using ZXing.Rendering;


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
        // DETALLES DE MIEMBRO
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (miembro == null) return NotFound();

            // Generar código de barras Code128 con ZXing + ImageSharp
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 80,
                    Margin = 1
                }
            };

            var pixelData = writer.Write(miembro.CodigoBarra);

            using var image = new Image<Rgba32>(pixelData.Width, pixelData.Height);

            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int idx = (y * pixelData.Width + x) * 4;
                    image[x, y] = new Rgba32(
                        pixelData.Pixels[idx],
                        pixelData.Pixels[idx + 1],
                        pixelData.Pixels[idx + 2],
                        pixelData.Pixels[idx + 3]);
                }
            }

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());

            ViewBag.Barcode = ms.ToArray();

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
                // --------------- Validación personalizada DNI ---------------
                if (miembro.TipoDocumento == "DNI" && (miembro.DNI == null || miembro.DNI.Length != 8))
                {
                    ModelState.AddModelError("DNI", "El DNI nacional debe tener exactamente 8 dígitos.");
                }
                else if (miembro.TipoDocumento == "DNI-Extranjero" && (miembro.DNI == null || miembro.DNI.Length != 9))
                {
                    ModelState.AddModelError("DNI", "El DNI extranjero debe tener exactamente 9 dígitos.");
                }
                else if (string.IsNullOrWhiteSpace(miembro.DNI))
                {
                    ModelState.AddModelError("DNI", "El campo DNI es obligatorio.");
                }

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
        // MÉTODOS PRIVADOS AUXILIARES
        // =====================================================
        private string GenerarCodigoBarra()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
        }

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
        // GENERAR PDF DEL CARNET DEL MIEMBRO
        // =====================================================
        public async Task<IActionResult> GenerarPDF(int id)
        {
            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (miembro == null)
                return NotFound();

            // Código de barras
            byte[] barcodeBytes;
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 80,
                    Margin = 0,
                    PureBarcode = true
                }
            };

            using (var image = writer.Write(miembro.CodigoBarra))
            {
                using var ms = new MemoryStream();
                image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                barcodeBytes = ms.ToArray();
            }

            // Foto opcional
            byte[]? fotoBytes = null;
            if (!string.IsNullOrEmpty(miembro.Foto))
            {
                var rutaFoto = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", miembro.Foto.TrimStart('/'));
                if (System.IO.File.Exists(rutaFoto))
                    fotoBytes = await System.IO.File.ReadAllBytesAsync(rutaFoto);
            }

            // Generar PDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A7);
                    page.Margin(10);
                    page.Background(Colors.Grey.Lighten3);

                    page.Content().Padding(5).Column(col =>
                    {
                        // Contenedor principal con borde
                        col.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(5).Column(innerCol =>
                        {
                            innerCol.Item().Text("CARNET DE SOCIO")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Medium)
                                .AlignCenter();

                            innerCol.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            innerCol.Item().Text($"DNI: {miembro.DNI}").FontSize(10).AlignCenter();
                            innerCol.Item().Text($"{miembro.Nombre} {miembro.Apellido}").FontSize(12).Bold().AlignCenter();
                            innerCol.Item().Text($"Tipo de Membresía: {miembro.Membresia?.Nombre}").FontSize(10).AlignCenter();
                            innerCol.Item().Text($"Fecha de alta: {miembro.FechaAlta:dd/MM/yyyy}").FontSize(10).AlignCenter();

                            if (fotoBytes != null)
                            {
                                innerCol.Item().AlignCenter().Image(fotoBytes);
                            }

                            innerCol.Item().AlignCenter().Image(barcodeBytes);
                        });
                    });
                });
            }).GeneratePdf();





            Response.Headers["Content-Disposition"] = $"inline; filename=Carnet_{miembro.CodigoBarra}.pdf";
            return File(pdfBytes, "application/pdf");
        }






    }
}
