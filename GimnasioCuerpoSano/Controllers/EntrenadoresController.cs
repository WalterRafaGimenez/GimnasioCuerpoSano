using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GimnasioCuerpoSano.Controllers
{
    public class EntrenadoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EntrenadoresController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public IActionResult Index()
        {
            var entrenadores = _context.Entrenadores.ToList();
            return View(entrenadores);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        public IActionResult Details(int id)
        {
            var entrenador = _context.Entrenadores.FirstOrDefault(e => e.Id == id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Entrenador entrenador, IFormFile? certificado)
        {
            // --- Validación DNI ---
            ValidarDNIEmail(entrenador);

            if (!ModelState.IsValid) return View(entrenador);

            // --- Manejo de certificado ---
            if (certificado != null && certificado.Length > 0)
            {
                entrenador.RutaCertificado = await GuardarCertificado(certificado);
            }

            _context.Entrenadores.Add(entrenador);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        public IActionResult Edit(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Entrenador entrenador, IFormFile? certificado)
        {
            if (id != entrenador.Id) return NotFound();

            ValidarDNIEmail(entrenador, id);

            if (!ModelState.IsValid) return View(entrenador);

            var entrenadorExistente = await _context.Entrenadores.FindAsync(id);
            if (entrenadorExistente == null) return NotFound();

            // --- Actualizar datos ---
            entrenadorExistente.Nombre = entrenador.Nombre;
            entrenadorExistente.Apellido = entrenador.Apellido;
            entrenadorExistente.Direccion = entrenador.Direccion;
            entrenadorExistente.Telefono = entrenador.Telefono;
            entrenadorExistente.Email = entrenador.Email;
            entrenadorExistente.Especialidad = entrenador.Especialidad;
            entrenadorExistente.FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado;

            // --- Manejo de certificado ---
            if (certificado != null && certificado.Length > 0)
            {
                // Eliminar anterior
                if (!string.IsNullOrEmpty(entrenadorExistente.RutaCertificado))
                {
                    var rutaVieja = Path.Combine(_environment.WebRootPath, entrenadorExistente.RutaCertificado.TrimStart('/'));
                    if (System.IO.File.Exists(rutaVieja))
                        System.IO.File.Delete(rutaVieja);
                }

                entrenadorExistente.RutaCertificado = await GuardarCertificado(certificado);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE (GET)
        // =====================================================
        public IActionResult Delete(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(entrenador.RutaCertificado))
                    {
                        var rutaArchivo = Path.Combine(_environment.WebRootPath, entrenador.RutaCertificado.TrimStart('/'));
                        if (System.IO.File.Exists(rutaArchivo))
                            System.IO.File.Delete(rutaArchivo);
                    }

                    _context.Entrenadores.Remove(entrenador);
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al eliminar: " + ex.Message;
                    return View(entrenador);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // MÉTODOS PRIVADOS AUXILIARES
        // =====================================================

        // Validar DNI y Email únicos y formato
        private void ValidarDNIEmail(Entrenador entrenador, int id = 0)
        {
            // Validación de DNI
            if (entrenador.TipoDocumento == "DNI" && (string.IsNullOrWhiteSpace(entrenador.DNI) || entrenador.DNI.Length != 8))
                ModelState.AddModelError("DNI", "El DNI nacional debe tener exactamente 8 dígitos.");
            else if (entrenador.TipoDocumento == "DNI-Extranjero" && (string.IsNullOrWhiteSpace(entrenador.DNI) || entrenador.DNI.Length != 9))
                ModelState.AddModelError("DNI", "El DNI extranjero debe tener exactamente 9 dígitos.");
            else if (string.IsNullOrWhiteSpace(entrenador.DNI))
                ModelState.AddModelError("DNI", "El campo DNI es obligatorio.");

            // Validación unicidad DNI
            if (_context.Entrenadores.Any(e => e.DNI == entrenador.DNI && e.Id != id))
                ModelState.AddModelError("DNI", "Ya existe otro entrenador con este DNI.");

            // Validación unicidad Email
            if (!string.IsNullOrWhiteSpace(entrenador.Email) && _context.Entrenadores.Any(e => e.Email == entrenador.Email && e.Id != id))
                ModelState.AddModelError("Email", "Ya existe otro entrenador con este correo electrónico.");
        }

        // Guardar archivo certificado
        private async Task<string> GuardarCertificado(IFormFile certificado)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "certificados2");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var nombreArchivo = Guid.NewGuid() + Path.GetExtension(certificado.FileName);
            var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await certificado.CopyToAsync(stream);

            return "/certificados2/" + nombreArchivo;
        }

        // =====================================================
        // VALIDACIONES AJAX
        // =====================================================
        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> VerificarDNI(string dni, int? id)
        {
            var existe = await _context.Entrenadores.AnyAsync(e => e.DNI == dni && e.Id != (id ?? 0));
            return Json(existe ? $"Ya existe otro entrenador con el DNI {dni}." : true);
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> VerificarEmail(string email, int? id)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(true);

            var existe = await _context.Entrenadores.AnyAsync(e => e.Email == email && e.Id != (id ?? 0));
            return Json(existe ? $"Ya existe otro entrenador con el correo {email}." : true);
        }

        // =====================================================
        // GUARDAR ARCHIVO OPCIONAL
        // =====================================================
        [HttpGet]
        public IActionResult GuardarArchivo() => View();

        [HttpPost]
        public async Task<IActionResult> GuardarArchivo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Mensaje = "No se seleccionó ningún archivo.";
                return View();
            }

            try
            {
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "certificados");
                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);

                var rutaArchivo = Path.Combine(rutaCarpeta, archivo.FileName);
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                    await archivo.CopyToAsync(stream);

                ViewBag.Mensaje = $"Archivo guardado correctamente en {rutaArchivo}";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al guardar el archivo: {ex.Message}";
            }

            return View();
        }

        // =====================================================
        // GENERAR PDF LISTADO DE ENTRENADORES - ABRIR EN NAVEGADOR
        // =====================================================
        public async Task<IActionResult> GenerarListadoPdf()
        {
            var entrenadores = await _context.Entrenadores
                .OrderBy(e => e.Apellido)
                .ThenBy(e => e.Nombre)
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
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(20).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Entrenadores")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // ============= CONTENIDO =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        // Columnas
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);  // Tipo Doc
                            columns.ConstantColumn(80);  // DNI
                            columns.RelativeColumn(2);   // Nombre
                            columns.RelativeColumn(2);   // Apellido
                            columns.RelativeColumn(2);   // Especialidad
                            columns.ConstantColumn(80);  // Teléfono
                            columns.RelativeColumn(3);   // Email
                            columns.ConstantColumn(80);  // Venc. Certificado
                        });

                        // Encabezado
                        table.Header(headerRow =>
                        {
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tipo Doc.").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("DNI").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nombre").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Apellido").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Especialidad").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Teléfono").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Email").Bold();
                            headerRow.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Venc. Certificado").Bold();
                        });

                        // Filas
                        foreach (var e in entrenadores)
                        {
                            table.Cell().Padding(5).Text(e.TipoDocumento);
                            table.Cell().Padding(5).Text(e.DNI);
                            table.Cell().Padding(5).Text(e.Nombre);
                            table.Cell().Padding(5).Text(e.Apellido);
                            table.Cell().Padding(5).Text(e.Especialidad);
                            table.Cell().Padding(5).Text(e.Telefono);
                            table.Cell().Padding(5).Text(e.Email);
                            table.Cell().Padding(5).Text(e.FechaVencimientoCertificado?.ToString("yyyy-MM-dd") ?? "");
                        }
                    });

                    // ============= PIE DE PÁGINA =============
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Line("© Gimnasio Cuerpo Sano " + DateTime.Now.Year);
                        x.Line("Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            })
            .GeneratePdf();

            // Abrir PDF directamente en nueva pestaña
            Response.Headers["Content-Disposition"] = "inline; filename=Listado_Entrenadores.pdf";
            return File(pdfBytes, "application/pdf");
        }

    }
}




