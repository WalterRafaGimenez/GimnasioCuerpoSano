using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GimnasioCuerpoSano.Controllers
{
    public class InscripcionClasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscripcionClasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: InscripcionClases
        public IActionResult Index(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                return View(); // Mostramos formulario para ingresar DNI
            }

            var miembro = _context.Miembros
                .FirstOrDefault(m => m.DNI == dni);

            if (miembro == null)
            {
                TempData["Error"] = "No se encontró un miembro con ese DNI.";
                return View();
            }

            // Validar membresía vigente
            bool membresiaValida = false;
            if (miembro.Membresia != null)
            {
                var fechaVencimiento = miembro.FechaAlta.AddMonths(miembro.Membresia.DuracionEnMeses);
                membresiaValida = fechaVencimiento > DateTime.Now;
            }
            if (!membresiaValida)
            {
                ViewBag.Miembro = miembro;
                ViewBag.MembresiaValida = false;
                return View(new List<InscripcionClaseViewModel>()); // retorna la vista vacía
            }

            // Obtener todas las clases con su horario y entrenador
            var horarios = _context.HorariosClase
                .Select(h => new InscripcionClaseViewModel
                {
                    Id = h.Id,
                    ClaseNombre = h.Clase.Nombre,
                    EntrenadorNombre = h.Clase.Entrenador.Nombre + " " + h.Clase.Entrenador.Apellido,
                    HoraInicio = h.HoraInicio,
                    HoraFin = h.HoraFin,
                    FechaInscripcion = DateTime.Now,
                    Estado = _context.InscripcionClase
                        .Any(i => i.HorarioClaseId == h.Id && i.MiembroId == miembro.Id && i.Estado)
                        ? "Inscripto"
                        : "No Inscripto"
                }).ToList();

            ViewBag.Miembro = miembro;
            ViewBag.MembresiaValida = membresiaValida;

            return View(horarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Guardar(int miembroId, List<InscripcionClaseViewModel> inscripciones)
        {
            var miembro = _context.Miembros.Find(miembroId);
            if (miembro == null)
                return NotFound();

            foreach (var i in inscripciones)
            {
                var inscripcion = _context.InscripcionClase
                    .FirstOrDefault(ic => ic.MiembroId == miembroId && ic.HorarioClaseId == i.Id);

                if (i.Estado == "Inscripto" && inscripcion == null)
                {
                    _context.InscripcionClase.Add(new InscripcionClase
                    {
                        MiembroId = miembroId,
                        HorarioClaseId = i.Id,
                        Estado = true,
                        FechaInscripcion = DateTime.Now
                    });
                }
                else if (i.Estado == "No Inscripto" && inscripcion != null)
                {
                    inscripcion.Estado = false;
                }
            }

            _context.SaveChanges();

            TempData["Mensaje"] = "Inscripciones actualizadas correctamente.";
            return RedirectToAction(nameof(Index), new { dni = miembro.DNI });
        }

        // =============================================
        // GENERAR PDF de clases en las que está inscripto un miembro
        // =============================================
        private byte[] GenerarPdfPorMiembro(Miembro miembro)
        {
            // Traemos las clases en las que está inscripto el miembro
            var inscripciones = _context.InscripcionClase
                .Where(i => i.MiembroId == miembro.Id && i.Estado)
                .Select(i => new
                {
                    ClaseNombre = i.HorarioClase.Clase.Nombre,
                    EntrenadorNombre = i.HorarioClase.Clase.Entrenador.Nombre + " " + i.HorarioClase.Clase.Entrenador.Apellido,
                    HoraInicio = i.HorarioClase.HoraInicio,
                    HoraFin = i.HorarioClase.HoraFin
                })
                .ToList();

            var stream = new MemoryStream();
            var rutaLogo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Logo.png");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);


                    // =================== HEADER ===================
                    page.Header().Row(row =>
                    {
                        row.ConstantItem(80).Image(rutaLogo);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignCenter().Text("Gimnasio Cuerpo Sano").Bold().FontSize(22).FontColor(Colors.Blue.Medium);
                            col.Item().AlignCenter().Text("Listado de Clases del Miembro").Bold().FontSize(16);
                            col.Item().AlignCenter().Text($"{miembro.Nombre} {miembro.Apellido}");
                            col.Item().AlignCenter().Text($"DNI: {miembro.DNI}");
                            col.Item().AlignCenter().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        });
                    });


                    // =================== CONTENT ===================
                    page.Content().Element(content =>
                    {
                        if (inscripciones.Any())
                        {
                            content.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeader).Text("Clase");
                                    header.Cell().Element(CellHeader).Text("Entrenador");
                                    header.Cell().Element(CellHeader).Text("Hora Inicio");
                                    header.Cell().Element(CellHeader).Text("Hora Fin");
                                });

                                foreach (var c in inscripciones)
                                {
                                    table.Cell().Element(CellBody).Text(c.ClaseNombre);
                                    table.Cell().Element(CellBody).Text(c.EntrenadorNombre);
                                    table.Cell().Element(CellBody).Text(c.HoraInicio.ToString(@"hh\:mm"));
                                    table.Cell().Element(CellBody).Text(c.HoraFin.ToString(@"hh\:mm"));
                                }
                            });
                        }
                        else
                        {
                            content.AlignCenter().Text("El miembro no está inscripto en ninguna clase actualmente.")
                                   .FontSize(14).Italic().FontColor(Colors.Grey.Medium);
                        }
                    });

                    // =================== FOOTER ===================
                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Página ").FontSize(10);
                        txt.CurrentPageNumber().FontSize(10);
                        txt.Span(" de ").FontSize(10);
                        txt.TotalPages().FontSize(10);
                    });

                    // ====== ESTILOS ======
                    static IContainer CellHeader(IContainer container) =>
                        container.Padding(5).Background(Colors.Grey.Lighten2).Border(1).AlignCenter();

                    static IContainer CellBody(IContainer container) =>
                        container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).AlignCenter();
                });
            });

            document.GeneratePdf(stream);
            return stream.ToArray();
        }
        // =============================================
        // ACCIÓN PÚBLICA -> Genera el PDF por DNI
        // =============================================
        [HttpGet]
        public IActionResult GenerarPdfPorMiembro(string dni)
        {
            if (string.IsNullOrEmpty(dni))
                return BadRequest("Debe ingresar un DNI válido.");

            // Buscamos al miembro
            var miembro = _context.Miembros.FirstOrDefault(m => m.DNI == dni);
            if (miembro == null)
                return NotFound("No se encontró un miembro con ese DNI.");

            // Generamos el PDF
            var pdfBytes = GenerarPdfPorMiembro(miembro);

            // --- INICIO DE MODIFICACIÓN ---

            // 1. Construir un nombre base para el archivo
            string nombreBase = $"Clases_{miembro.Apellido}_{miembro.Nombre}";

            // 2. Limpiar el nombre del archivo para eliminar caracteres no-ASCII
            // Esto previene el error "Invalid non-ASCII or control character in header"
            string nombreLimpio = nombreBase
                .Replace("ñ", "n").Replace("Ñ", "N")
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
                // Opcional: reemplazar espacios por guiones bajos, aunque no causa el error, es buena práctica
                .Replace(" ", "_");

            // 3. Lo mostramos en nueva pestaña usando el nombre limpio
            Response.Headers.Add("Content-Disposition", $"inline; filename={nombreLimpio}.pdf");

            // --- FIN DE MODIFICACIÓN ---

            return File(pdfBytes, "application/pdf");
        }

    }
}