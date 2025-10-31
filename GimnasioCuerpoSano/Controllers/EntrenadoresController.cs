using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    // Nota: las autorizaciones por rol se siguen respetando. El login aquí crea una cookie
    // con rol "Entrenador" para que [Authorize(Roles = "Entrenador")] funcione.
    public class EntrenadoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EntrenadoresController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize(Roles = "Entrenador")]
        public async Task<IActionResult> MisClases()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Account");

            var entrenador = await _context.Entrenadores
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (entrenador == null)
                return NotFound("Entrenador no encontrado");

            // Traemos las clases del entrenador con horarios y sala
            var clases = await _context.HorariosClase
                .Include(h => h.Clase)
                .Include(h => h.Sala)
                .Where(h => h.Clase.EntrenadorId == entrenador.Id)
                .Select(h => new ClaseEntrenadorViewModel
                {
                    NombreClase = h.Clase.Nombre!,
                    SalaNombre = h.Sala!.Nombre ?? h.Sala.Numero,
                    DiaSemana = h.DiaSemana.ToString(), // luego podemos formatear con Display(Name="")
                    HoraInicio = h.HoraInicio,
                    HoraFin = h.HoraFin,
                    DuracionMinutos = h.Clase.DuracionMinutos
                })
                .ToListAsync();

            // Creamos el ViewModel completo que la vista espera
            var model = new MisClasesEntrenadorViewModel
            {
                FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado,
                Clases = clases
            };

            return View(model);
        }




        // =====================================================
        // INDEX
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
        public IActionResult Index()
        {
            var entrenadores = _context.Entrenadores.ToList();
            return View(entrenadores);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
        public IActionResult Details(int id)
        {
            var entrenador = _context.Entrenadores.FirstOrDefault(e => e.Id == id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        [HttpPost]
        [Authorize(Roles = "Administrador,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Entrenador entrenador, IFormFile? certificado)
        {
            // Validaciones
            ValidarDNIEmail(entrenador);
            if (!ModelState.IsValid) return View(entrenador);

            // Guardar certificado
            if (certificado != null && certificado.Length > 0)
                entrenador.RutaCertificado = await GuardarCertificado(certificado);

            _context.Entrenadores.Add(entrenador);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Entrenador '{entrenador.Nombre} {entrenador.Apellido}' creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
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
        [Authorize(Roles = "Administrador,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Entrenador entrenador, IFormFile? certificado)
        {
            if (id != entrenador.Id) return NotFound();

            ValidarDNIEmail(entrenador, id);
            if (!ModelState.IsValid) return View(entrenador);

            var entrenadorExistente = await _context.Entrenadores.FindAsync(id);
            if (entrenadorExistente == null) return NotFound();

            // Actualizar campos
            entrenadorExistente.Nombre = entrenador.Nombre;
            entrenadorExistente.Apellido = entrenador.Apellido;
            entrenadorExistente.Direccion = entrenador.Direccion;
            entrenadorExistente.Telefono = entrenador.Telefono;
            entrenadorExistente.Email = entrenador.Email;
            entrenadorExistente.Especialidad = entrenador.Especialidad;
            entrenadorExistente.FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado;

            // Manejo de certificado (reemplazo)
            if (certificado != null && certificado.Length > 0)
            {
                if (!string.IsNullOrEmpty(entrenadorExistente.RutaCertificado))
                {
                    var rutaVieja = Path.Combine(_environment.WebRootPath, entrenadorExistente.RutaCertificado.TrimStart('/'));
                    if (System.IO.File.Exists(rutaVieja))
                        System.IO.File.Delete(rutaVieja);
                }
                entrenadorExistente.RutaCertificado = await GuardarCertificado(certificado);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Entrenador '{entrenadorExistente.Nombre} {entrenadorExistente.Apellido}' actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE (GET)
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
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
        [Authorize(Roles = "Administrador,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrenador = await _context.Entrenadores.FindAsync(id);
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
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Entrenador eliminado correctamente.";
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
        // PERFIL DEL ENTRENADOR (requiere rol Entrenador)
        // =====================================================
        [Authorize(Roles = "Entrenador")]
        public async Task<IActionResult> Perfil()
        {
            // Usamos User.Identity.Name (email) para buscar al entrenador
            var email = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

            var entrenador = await _context.Entrenadores
                .Include(e => e.Clases) // si agregaste la navegación Clases
                    .ThenInclude(c => c.Sala)
                .FirstOrDefaultAsync(e => e.Email == email);

            if (entrenador == null) return NotFound();

            // Podés enviar un ViewModel si querés clases filtradas por horarios, etc.
            return View(entrenador);
        }

        // =====================================================
        // LOGIN ENTRENADOR (DNI + Email) - AllowAnonymous
        // =====================================================
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string dni, string email, string returnUrl = null)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "DNI y Email son obligatorios.";
                return View();
            }

            var entrenador = await _context.Entrenadores.FirstOrDefaultAsync(e => e.DNI == dni && e.Email == email);
            if (entrenador == null)
            {
                ViewBag.Error = "Entrenador no encontrado en la base de datos.";
                return View();
            }

            // Validar certificado vigente
            if (entrenador.FechaVencimientoCertificado.HasValue && entrenador.FechaVencimientoCertificado.Value < DateTime.Now.Date)
            {
                ViewBag.Error = "El certificado del entrenador está vencido.";
                return View();
            }

            // Si pasó validaciones, creamos claims y firmamos cookie con rol Entrenador
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, entrenador.Email ?? ""),
                new Claim(ClaimTypes.Email, entrenador.Email ?? ""),
                new Claim(ClaimTypes.Role, "Entrenador"),
                new Claim("EntrenadorId", entrenador.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false
            });

            // Redirigir al perfil o a returnUrl
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Perfil));
        }

        // =====================================================
        // LOGOUT (para entrenadores logueados con esta cookie)
        // =====================================================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // VALIDACIONES AJAX (mantengo las tuyas)
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
            if (string.IsNullOrWhiteSpace(email)) return Json(true);
            var existe = await _context.Entrenadores.AnyAsync(e => e.Email == email && e.Id != (id ?? 0));
            return Json(existe ? $"Ya existe otro entrenador con el correo {email}." : true);
        }

        // =====================================================
        // GUARDAR CERTIFICADO (archivo)
        // =====================================================
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
        // GENERAR PDF LISTADO DE ENTRENADORES
        // =====================================================
        [Authorize(Roles = "Administrador,Empleado")]
        public async Task<IActionResult> GenerarListadoPdf()
        {
            var entrenadores = await _context.Entrenadores
                .OrderBy(e => e.Apellido)
                .ThenBy(e => e.Nombre)
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

                        header.Item().PaddingTop(5).Text("Listado de Entrenadores")
                            .FontSize(16).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // CONTENIDO
                    page.Content().PaddingVertical(15).Table(table =>
                    {
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

                    // PIE DE PÁGINA
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Line("© Gimnasio Cuerpo Sano " + DateTime.Now.Year);
                        x.Line("Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            }).GeneratePdf();

            Response.Headers["Content-Disposition"] = "inline; filename=Listado_Entrenadores.pdf";
            return File(pdfBytes, "application/pdf");
        }

        // =====================================================
        // MÉTODOS PRIVADOS AUXILIARES (validaciones)
        // =====================================================
        private void ValidarDNIEmail(Entrenador entrenador, int id = 0)
        {
            if (entrenador.TipoDocumento == "DNI" && (string.IsNullOrWhiteSpace(entrenador.DNI) || entrenador.DNI.Length != 8))
                ModelState.AddModelError("DNI", "El DNI nacional debe tener exactamente 8 dígitos.");
            else if (entrenador.TipoDocumento == "DNI-Extranjero" && (string.IsNullOrWhiteSpace(entrenador.DNI) || entrenador.DNI.Length != 9))
                ModelState.AddModelError("DNI", "El DNI extranjero debe tener exactamente 9 dígitos.");
            else if (string.IsNullOrWhiteSpace(entrenador.DNI))
                ModelState.AddModelError("DNI", "El campo DNI es obligatorio.");

            if (_context.Entrenadores.Any(e => e.DNI == entrenador.DNI && e.Id != id))
                ModelState.AddModelError("DNI", "Ya existe otro entrenador con este DNI.");

            if (!string.IsNullOrWhiteSpace(entrenador.Email) && _context.Entrenadores.Any(e => e.Email == entrenador.Email && e.Id != id))
                ModelState.AddModelError("Email", "Ya existe otro entrenador con este correo electrónico.");
        }
    }
}

