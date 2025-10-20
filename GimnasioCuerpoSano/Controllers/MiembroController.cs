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

        // GET: Miembro/CheckDNI
        public JsonResult CheckDNI(string dni)
        {
            var exists = _context.Miembros.Any(m => m.DNI == dni);
            return Json(new { exists });
        }

        // GET: Miembro/CheckMail
        public JsonResult CheckMail(string mail)
        {
            var exists = _context.Miembros.Any(m => m.Mail == mail);
            return Json(new { exists });
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
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (miembro == null) return NotFound();

            // Código de barras
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions { Width = 300, Height = 80, Margin = 1 }
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
            // Cargar lista de membresías para el dropdown
            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");

            // Crear objeto vacío para la vista
            var miembro = new Miembro();

            return View(miembro);
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

                // --------------- Validación de unicidad DNI y Mail ---------------
                if (await _context.Miembros.AnyAsync(m => m.DNI == miembro.DNI))
                {
                    ModelState.AddModelError("DNI", "Ya existe un miembro registrado con este DNI.");
                }

                if (!string.IsNullOrWhiteSpace(miembro.Mail) &&
                    await _context.Miembros.AnyAsync(m => m.Mail == miembro.Mail))
                {
                    ModelState.AddModelError("Mail", "Ya existe un miembro registrado con este correo electrónico.");
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
                    return View(miembro);
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
                    ModelState.AddModelError("", "No se pudo generar un código de barras único. Intente nuevamente.");
                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                    return View(miembro);
                }

                miembro.CodigoBarra = codigoGenerado;
                Console.WriteLine("Código de barras final asignado: " + miembro.CodigoBarra);

                // --------------- Agregar y guardar ---------------
                _context.Miembros.Add(miembro);
                await _context.SaveChangesAsync();

                TempData["UltimoMiembroId"] = miembro.Id;
                TempData["Mensaje"] = $"Miembro registrado correctamente. Código de barra: {miembro.CodigoBarra}";
                Console.WriteLine("Create POST finalizado con éxito. Id miembro: " + miembro.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Excepción al crear miembro: " + ex.ToString());
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Miembro miembro, IFormFile? fotoArchivo)
        {
            if (id != miembro.Id)
                return NotFound();

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);

            // --- Validación DNI ---
            if (miembro.TipoDocumento == "DNI" && (miembro.DNI == null || miembro.DNI.Length != 8))
                ModelState.AddModelError("DNI", "El DNI nacional debe tener exactamente 8 dígitos.");
            else if (miembro.TipoDocumento == "DNI-Extranjero" && (miembro.DNI == null || miembro.DNI.Length != 9))
                ModelState.AddModelError("DNI", "El DNI extranjero debe tener exactamente 9 dígitos.");
            else if (string.IsNullOrWhiteSpace(miembro.DNI))
                ModelState.AddModelError("DNI", "El campo DNI es obligatorio.");

            // --- Validación unicidad DNI y Mail ---
            if (await _context.Miembros.AnyAsync(m => m.DNI == miembro.DNI && m.Id != miembro.Id))
                ModelState.AddModelError("DNI", "Ya existe otro miembro registrado con este DNI.");

            if (!string.IsNullOrWhiteSpace(miembro.Mail) &&
                await _context.Miembros.AnyAsync(m => m.Mail == miembro.Mail && m.Id != miembro.Id))
                ModelState.AddModelError("Mail", "Ya existe otro miembro registrado con este correo electrónico.");

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

            // --- Manejo de foto ---
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

            // --- Código de barra ---
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
        // MÉTODOS AJAX PARA VALIDAR UNICIDAD EN TIEMPO REAL
        // =====================================================
        [HttpGet]
        public async Task<JsonResult> CheckDNI(string dni, int id)
        {
            var isUnique = !await _context.Miembros.AnyAsync(m => m.DNI == dni && m.Id != id);
            return Json(new { isUnique });
        }

        [HttpGet]
        public async Task<JsonResult> CheckMail(string mail, int id)
        {
            var isUnique = !await _context.Miembros.AnyAsync(m => m.Mail == mail && m.Id != id);
            return Json(new { isUnique });
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
                   //PureBarcode = true
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
            float mmToPoints(float mm) => mm * 72f / 25.4f; // 1 pulgada = 25.4 mm, 1 pulgada = 72 pt

            float width = mmToPoints(85);  // 85 mm
            float height = mmToPoints(54); // 54 mm

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(width, height); // tamaño carnet
                    page.Margin(5);
                    page.Background(QuestPDF.Helpers.Colors.White);

                    page.Content().Padding(5).Column(col =>
                    {
                        col.Spacing(2);
                        col.Item().Text("CARNET DE SOCIO")
                            .FontSize(10)
                            .Bold()
                            .AlignCenter();

                        col.Item().LineHorizontal(1)
                            .LineColor(QuestPDF.Helpers.Colors.Grey.Medium);

                        col.Item().Text($"DNI: {miembro.DNI}").FontSize(8).AlignCenter();
                        col.Item().Text($"{miembro.Nombre} {miembro.Apellido}")
                            .FontSize(10)
                            .Bold()
                            .AlignCenter();
                        col.Item().Text($"Tipo de Membresía: {miembro.Membresia?.Nombre}")
                            .FontSize(8)
                            .AlignCenter();
                        col.Item().Text($"Fecha de alta: {miembro.FechaAlta:dd/MM/yyyy}")
                            .FontSize(8)
                            .AlignCenter();

                        if (fotoBytes != null)
                            col.Item().AlignCenter().Image(fotoBytes, ImageScaling.FitArea);

                        col.Item().AlignCenter().Image(barcodeBytes, ImageScaling.FitWidth);
                    });
                });
            }).GeneratePdf();

            Response.Headers["Content-Disposition"] = $"inline; filename=Carnet_{miembro.CodigoBarra}.pdf";
            return File(pdfBytes, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ImprimirListado()
        {
            var miembros = await _context.Miembros
                .Include(m => m.Membresia)
                .OrderBy(m => m.Apellido)
                .ToListAsync();

            if (miembros == null || !miembros.Any())
            {
                return Content("No hay miembros registrados para imprimir.");
            }

            QuestPDF.Settings.License = LicenseType.Community;

            // Ruta del logo (ajustala si tu imagen está en otra carpeta)
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo.png");
            var logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);

                    /// ============= CABECERA =============
                    
                    page.Header().Row(header =>
                    {
                        // Columna izquierda: logo
                        if (logoBytes != null)
                        {
                            header.RelativeColumn(1)
                                  .AlignLeft()
                                  .Container()
                                  .Width(100)
                                  .Image(logoBytes);
                        }
                        else
                        {
                            header.RelativeColumn(1); // espacio vacío si no hay logo
                        }

                        // Columna derecha: textos alineados a la derecha
                        header.RelativeColumn(3).AlignRight().Column(col =>
                        {
                            col.Item().Text("Gimnasio Cuerpo Sano")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Medium)
                                .AlignRight();

                            col.Item().Text("Listado de Miembros")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.Grey.Darken1)
                                .AlignRight();

                            col.Item().PaddingTop(5).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2)
                                .AlignRight();
                        });
                    });



                    // ============= CONTENIDO =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60); // DNI
                            columns.RelativeColumn(1);  // Nombre
                            columns.RelativeColumn(1);  // Apellido
                            columns.RelativeColumn(1);  // Mail
                            columns.RelativeColumn(1);  // Membresía
                            columns.RelativeColumn(1);  // Fecha Alta
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyleHeader).Text("DNI");
                            header.Cell().Element(CellStyleHeader).Text("Nombre");
                            header.Cell().Element(CellStyleHeader).Text("Apellido");
                            header.Cell().Element(CellStyleHeader).Text("Mail");
                            header.Cell().Element(CellStyleHeader).Text("Membresía");
                            header.Cell().Element(CellStyleHeader).Text("Fecha Alta");

                            static IContainer CellStyleHeader(IContainer container) =>
                                container.DefaultTextStyle(x => x.Bold().FontColor(Colors.White))
                                         .Background(Colors.Blue.Medium)
                                         .PaddingVertical(5)
                                         .PaddingHorizontal(3)
                                         .BorderBottom(1)
                                         .BorderColor(Colors.Grey.Lighten1);
                        });

                        // Filas
                        bool alternar = false;
                        foreach (var m in miembros)
                        {
                            var fondo = alternar ? Colors.Grey.Lighten4 : Colors.White;
                            alternar = !alternar;

                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.DNI);
                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.Nombre);
                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.Apellido);
                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.Mail ?? "-");
                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.Membresia?.Nombre ?? "-");
                            table.Cell().Element(c => CellStyle(c, fondo)).Text(m.FechaAlta.ToString("dd/MM/yyyy"));
                        }

                        static IContainer CellStyle(IContainer container, string background) =>
                            container.Background(background)
                                     .PaddingVertical(4)
                                     .PaddingHorizontal(3)
                                     .BorderBottom(0.5f)
                                     .BorderColor(Colors.Grey.Lighten2);
                    });

                    // ============= PIE DE PÁGINA =============
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generado el ").FontSize(9);
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).Bold();
                    });
                });
            })
            .GeneratePdf();

            // Mostramos el PDF directamente en el navegador
            Response.Headers.Add("Content-Disposition", "inline; filename=ListadoMiembros.pdf");
            return File(pdfBytes, "application/pdf");
        }





    }
}
