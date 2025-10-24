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
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;

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
        // AJAX: Validación DNI y Mail en tiempo real
        // =====================================================
        [HttpGet]
        public async Task<JsonResult> CheckDNI(string dni, int? id = null)
        {
            var exists = id.HasValue
                ? await _context.Miembros.AnyAsync(m => m.DNI == dni && m.Id != id.Value)
                : await _context.Miembros.AnyAsync(m => m.DNI == dni);
            return Json(new { exists });
        }

        [HttpGet]
        public async Task<JsonResult> CheckMail(string mail, int? id = null)
        {
            var exists = id.HasValue
                ? await _context.Miembros.AnyAsync(m => m.Mail == mail && m.Id != id.Value)
                : await _context.Miembros.AnyAsync(m => m.Mail == mail);
            return Json(new { exists });
        }

        // =====================================================
        // LISTADO DE MIEMBROS
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

            // Generar Barcode para mostrar
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions { Width = 300, Height = 80, Margin = 1 }
            };
            var pixelData = writer.Write(miembro.CodigoBarra);
            using var image = new Image<Rgba32>(pixelData.Width, pixelData.Height);
            for (int y = 0; y < pixelData.Height; y++)
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int idx = (y * pixelData.Width + x) * 4;
                    image[x, y] = new Rgba32(pixelData.Pixels[idx], pixelData.Pixels[idx + 1], pixelData.Pixels[idx + 2], pixelData.Pixels[idx + 3]);
                }

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            ViewBag.Barcode = ms.ToArray();

            return View(miembro);
        }

        // =====================================================
        // CREAR MIEMBRO (GET)
        // =====================================================
        public IActionResult Create()
        {
            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
            return View(new Miembro());
        }

        // =====================================================
        // CREAR MIEMBRO (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Miembro miembro, IFormFile? fotoArchivo)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                return View(miembro);
            }

            // Asignar valores
            var membresia = await _context.Membresias.FindAsync(miembro.MembresiaId);
            if (membresia == null)
            {
                ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                return View(miembro);
            }

            miembro.ValorMembresia = miembro.DescuentoEspecial ? membresia.Precio * 0.85m : membresia.Precio;
            miembro.FechaAlta = DateTime.Now;

            //Nuevo: calcular FechaVencimiento
            miembro.FechaVencimiento = DateTime.Now.AddMonths(membresia.DuracionEnMeses);



            // =====================================================
            // FOTO OBLIGATORIA (solo en creación)
            // =====================================================
            if (fotoArchivo == null || fotoArchivo.Length == 0)
            {
                ModelState.AddModelError("Foto", "Debe subir una foto obligatoriamente.");
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
                return View(miembro);
            }

            // Guardar foto si se adjunta
            if (fotoArchivo != null && fotoArchivo.Length > 0)
            {
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos");
                if (!Directory.Exists(rutaCarpeta)) Directory.CreateDirectory(rutaCarpeta);

                var nombreArchivo = Guid.NewGuid() + Path.GetExtension(fotoArchivo.FileName);
                var rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);

                using var stream = new FileStream(rutaArchivo, FileMode.Create);
                await fotoArchivo.CopyToAsync(stream);

                miembro.Foto = "/fotos/" + nombreArchivo;
            }

            // =====================================================
            // Generar código de barras único
            // =====================================================
            miembro.CodigoBarra = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

            _context.Miembros.Add(miembro);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Miembro '{miembro.Nombre} {miembro.Apellido}' registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // EDITAR MIEMBRO (GET)
        // =====================================================
        public async Task<IActionResult> Edit(int id)
        {
            var miembro = await _context.Miembros.FindAsync(id);
            if (miembro == null) return NotFound();

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
            return View(miembro);
        }

        // =====================================================
        // EDITAR MIEMBRO (POST) - FOTO OPCIONAL SI YA EXISTE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Miembro miembro, IFormFile? fotoArchivo)
        {
            if (id != miembro.Id) return NotFound();

            // 1. Obtener el miembro existente
            var miembroExistente = await _context.Miembros
                .AsNoTracking()
                .Include(m => m.Membresia) // para tener duración
                .FirstOrDefaultAsync(m => m.Id == id);

            if (miembroExistente == null) return NotFound();

            // Mantener datos originales
            miembro.Foto = miembroExistente.Foto;
            miembro.CodigoBarra = miembroExistente.CodigoBarra;
            miembro.FechaAlta = miembroExistente.FechaAlta;

            // 2. Validación del modelo
            if (!ModelState.IsValid)
            {
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
                return View(miembro);
            }

            // 3. Validación y cálculo de membresía
            var membresiaNueva = await _context.Membresias.FindAsync(miembro.MembresiaId);
            if (membresiaNueva == null)
            {
                ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
                return View(miembro);
            }

           
            // 4. Actualizar FechaVencimiento si cambia la membresía
            if (miembro.MembresiaId != miembroExistente.MembresiaId)
            {
                // La nueva fecha de vencimiento se calcula desde hoy
                miembro.FechaVencimiento = DateTime.Now.AddMonths(membresiaNueva.DuracionEnMeses);
            }
            else
            {
                // Mantener la fecha de vencimiento actual
                miembro.FechaVencimiento = miembroExistente.FechaVencimiento;
            }

            // 5. Cálculo del precio
            miembro.ValorMembresia = miembro.DescuentoEspecial
                ? membresiaNueva.Precio * 0.85m
                : membresiaNueva.Precio;

            // 6. Lógica de FOTO
            if (fotoArchivo != null && fotoArchivo.Length > 0)
            {
                if (!string.IsNullOrEmpty(miembroExistente.Foto))
                {
                    var rutaAnterior = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", miembroExistente.Foto.TrimStart('/'));
                    if (System.IO.File.Exists(rutaAnterior))
                        System.IO.File.Delete(rutaAnterior);
                }

                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos");
                if (!Directory.Exists(rutaCarpeta)) Directory.CreateDirectory(rutaCarpeta);

                var nombreArchivo = Guid.NewGuid() + Path.GetExtension(fotoArchivo.FileName);
                var rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);

                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await fotoArchivo.CopyToAsync(stream);
                }

                miembro.Foto = "/fotos/" + nombreArchivo;
            }

            // 7. Validar foto obligatoria
            if (string.IsNullOrEmpty(miembro.Foto))
            {
                ModelState.AddModelError("Foto", "La foto es obligatoria para este miembro.");
                ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
                return View(miembro);
            }

            // 8. Guardar cambios
            _context.Entry(miembro).State = EntityState.Modified;

            // No permitir modificar la fecha de alta
            _context.Entry(miembro).Property(m => m.FechaAlta).IsModified = false;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Miembro '{miembro.Nombre} {miembro.Apellido}' actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // ELIMINAR MIEMBRO
        // =====================================================
        public async Task<IActionResult> Delete(int id)
        {
            var miembro = await _context.Miembros.Include(m => m.Membresia).FirstOrDefaultAsync(m => m.Id == id);
            if (miembro == null) return NotFound();
            return View(miembro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var miembro = await _context.Miembros.FindAsync(id);
            if (miembro != null)
            {
                _context.Miembros.Remove(miembro);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Miembro eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // PERFIL PERSONAL DEL MIEMBRO
        // =====================================================
        [Authorize(Roles = "Miembro,Administrador,Empleado")]
        public async Task<IActionResult> Perfil()
        {
            var email = User.Identity?.Name;
            if (email == null) return Unauthorized();

            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.Mail == email);

            if (miembro == null) return NotFound();

            return View(miembro);
        }

        //IMPRIMIR POR PDF LISTA
        public async Task<IActionResult> ImprimirListado()
        {
            var miembros = await _context.Miembros
                .Include(m => m.Membresia)
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
                        {
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);
                        }

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Miembros")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // ============= CONTENIDO =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                           // columns.ConstantColumn(30);   // ID
                            columns.RelativeColumn(2);    // Nombre
                            columns.RelativeColumn(2);    // Apellido
                            columns.RelativeColumn(2);    // DNI
                            columns.RelativeColumn(3);    // Mail
                            columns.RelativeColumn(2);    // Teléfono
                            columns.RelativeColumn(2);    // Tipo Membresía
                           // columns.RelativeColumn(1);    // Valor
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                           // header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("ID").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Apellido").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DNI").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Mail").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Teléfono").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Membresía").Bold();
                           // header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Valor").Bold();
                        });

                        // Filas
                        foreach (var m in miembros)
                        {
                            //table.Cell().Padding(5).Text(m.Id.ToString());
                            table.Cell().Padding(5).Text(m.Nombre);
                            table.Cell().Padding(5).Text(m.Apellido);
                            table.Cell().Padding(5).Text(m.DNI);
                            table.Cell().Padding(5).Text(m.Mail);
                            table.Cell().Padding(5).Text(m.Telefono);
                            table.Cell().Padding(5).Text(m.Membresia?.Nombre ?? "Sin Membresía");
                           // table.Cell().Padding(5).Text($"${m.ValorMembresia:F2}");
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
            Response.Headers.Add("Content-Disposition", "inline; filename=ListadoMiembros.pdf");
            return File(pdfBytes, "application/pdf");
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

            // 1. GENERACION DEL CODIGO DE BARRAS
            // Es recomendable que las opciones de ZXing no sean demasiado grandes 
            // ya que QuestPDF las escalará en base a su resolución intrínseca.
            byte[] barcodeBytes;
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 250, // Ajustado
                    Height = 70, // Ajustado
                    Margin = 0,
                }
            };

            using (var image = writer.Write(miembro.CodigoBarra))
            {
                using var ms = new MemoryStream();
                image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                barcodeBytes = ms.ToArray();
            }

            // 2. LECTURA DE LA FOTO
            byte[]? fotoBytes = null;
            if (!string.IsNullOrEmpty(miembro.Foto))
            {
                var rutaFoto = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", miembro.Foto.TrimStart('/'));
                if (System.IO.File.Exists(rutaFoto))
                    fotoBytes = await System.IO.File.ReadAllBytesAsync(rutaFoto);
            }

            // 3. GENERACION DEL PDF (Ajustado para Carnet)
            // Helper function to convert millimeters to PDF points (1 inch = 72 points, 1 inch = 25.4 mm)
            float mmToPoints(float mm) => mm * 72f / 25.4f;

            // CAMBIO CLAVE: Dimensiones VERTICALES (54mm ancho x 85mm alto)
            float width = mmToPoints(54);  // Ancho de la tarjeta
            float height = mmToPoints(85); // Alto de la tarjeta

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(width, height); // Establece el tamaño de la página a 54x85 mm
                    page.Margin(mmToPoints(2)); // Márgenes reducidos a 2mm
                    page.Background(QuestPDF.Helpers.Colors.White);

                    page.Content().Padding(mmToPoints(1)).Column(col => // Padding adicional en el contenido
                    {
                        // El espaciado de 1.5 a 2 mm es clave para que quepa todo
                        col.Spacing(mmToPoints(1.5f));

                        // A. ENCABEZADO Y TITULO
                        col.Item().Text("CARNET DE SOCIO").FontSize(7).Bold().AlignCenter();
                        col.Item().LineHorizontal(0.5f).LineColor(QuestPDF.Helpers.Colors.Grey.Medium);

                        // C. FOTO Y DETALLES DEL MIEMBRO
                        col.Item().Column(memberDetailsCol =>
                        {
                            // 1. FOTO (Centrada)
                            if (fotoBytes != null)
                            {
                                // Fija el tamaño de la foto a 20x25 mm (tamaño de carnet pequeño)
                                float photoSize = mmToPoints(22); // Aumentamos un poco la foto
                                memberDetailsCol.Item().AlignCenter().Width(photoSize).Height(photoSize)
                                    .Image(fotoBytes, ImageScaling.FitArea);
                            }

                            // 2. COLUMNA DE DETALLES DEL MIEMBRO
                            memberDetailsCol.Item().PaddingTop(mmToPoints(1)).Column(textCol =>
                            {
                                textCol.Spacing(1f); // Espaciado mínimo entre líneas de texto
                                                     // Centramos el texto
                                textCol.Item().Text($"{miembro.Nombre} {miembro.Apellido}").FontSize(8).Bold().AlignCenter();
                                textCol.Item().Text($"DNI: {miembro.DNI}").FontSize(6).AlignCenter();
                                textCol.Item().Text($"Membresía: {miembro.Membresia?.Nombre}").FontSize(6).AlignCenter();
                                textCol.Item().Text($"Alta: {miembro.FechaAlta:dd/MM/yyyy}").FontSize(6).AlignCenter();
                            });
                        });

                        // D. ESPACIADOR (Empuja el código de barras al fondo si el contenido es corto)
                        // CORRECCIÓN CLAVE: Quitamos ExtendVertical() que causaba el desborde a la 2da página.
                        // Usamos un pequeño Spacer para separar el contenido superior del código de barras.
                        col.Item().PaddingTop(mmToPoints(3));

                        // E. CODIGO DE BARRAS (Al final de la columna principal)
                        col.Item().AlignCenter().PaddingBottom(mmToPoints(0)) // Margen inferior nulo
                            .Column(barcodeCol =>
                            {
                                // Imagen del Código de Barras
                                barcodeCol.Item().AlignCenter()
                                    .Container().Height(mmToPoints(8)) // Altura estricta para la imagen
                                    .AlignCenter().Image(barcodeBytes, ImageScaling.FitArea);

                                // Número del Código de Barras
                                barcodeCol.Item().Text(miembro.CodigoBarra)
                                    .FontSize(5) // Tamaño de fuente muy pequeño para caber
                                    .AlignCenter();
                            });
                    });
                });
            }).GeneratePdf();

            Response.Headers["Content-Disposition"] = $"inline; filename=Carnet_{miembro.CodigoBarra}.pdf";
            return File(pdfBytes, "application/pdf");
        }









        // =====================================================
        // FUTURO: Acciones para Actividades (anotarse, baja)
        // =====================================================
        // Aquí se podrán agregar acciones que filtren por miembro y por rol de entrenador



    }
}

