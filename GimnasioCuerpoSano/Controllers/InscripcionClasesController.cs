using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
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

        //CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(InscripcionClase inscripcion)
        {
            if (!ModelState.IsValid) return View(inscripcion);

            _context.InscripcionClase.Add(inscripcion);
            _context.SaveChanges();
            TempData["Mensaje"] = "Inscripción creada correctamente.";
            return RedirectToAction(nameof(Index));
        }


    
        // GET: InscripcionClases
        public IActionResult Index(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                // Si no se ingresó DNI, mostramos solo el formulario
                return View();
            }

            // Buscamos el miembro con su membresía incluida
            var miembro = _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefault(m => m.DNI == dni);

            if (miembro == null)
            {
                TempData["Error"] = "No se encontró un miembro con ese DNI.";
                return View();
            }

            // Validamos membresía vigente
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
                return View(new List<InscripcionClaseViewModel>());
            }

            // Traemos horarios a memoria para evitar errores de EF con TimeSpan y DataReader
            var horariosDb = _context.HorariosClase
                .Include(h => h.Clase)
                    .ThenInclude(c => c.Entrenador)
                .Include(h => h.Sala)
                .Where(h => h.SePuedeInscribir)
                .ToList(); // Trae todos los registros a memoria

            // Convertimos a ViewModel y agregamos InscripcionId
            var horarios = horariosDb.Select(h =>
            {
                var inscripcion = _context.InscripcionClase
                    .FirstOrDefault(i => i.HorarioClaseId == h.Id && i.MiembroId == miembro.Id);

                return new InscripcionClaseViewModel
                {
                    Id = h.Id, // HorarioClaseId
                    InscripcionId = inscripcion?.Id, // null si no está inscripto
                    NombreClase = h.Clase.Nombre,
                    NombreEntrenador = h.Clase.Entrenador != null
                        ? h.Clase.Entrenador.Nombre + " " + h.Clase.Entrenador.Apellido
                        : "Sin entrenador",
                    HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                    DiaSemana = h.DiaSemana.ToString(),
                    FechaInscripcion = inscripcion?.FechaInscripcion ?? DateTime.Now,
                    Estado = (inscripcion != null && inscripcion.Estado) ? "Inscripto" : "No Inscripto"
                };
            })
            .OrderBy(h => h.NombreClase)
            .ThenBy(h => h.HoraInicio)
            .ToList();

            // Pasamos información al View
            ViewBag.Miembro = miembro;
            ViewBag.MembresiaValida = membresiaValida;

            return View(horarios);
        }


        // GET: InscripcionClases/Details/5
        public async Task<IActionResult> Details(int? id, string? dni)
        {
            if (id == null) return NotFound();

            var inscripcion = await _context.InscripcionClase
                .Include(i => i.Miembro)
                .Include(i => i.HorarioClase)
                    .ThenInclude(h => h.Clase)
                .Include(i => i.HorarioClase)
                    .ThenInclude(h => h.Sala)
                .Include(i => i.HorarioClase)
                    .ThenInclude(h => h.Clase.Entrenador)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inscripcion == null) return NotFound();

            var viewModel = new InscripcionClaseViewModel
            {
                Id = inscripcion.Id,
                NombreMiembro = inscripcion.Miembro?.Nombre,
                ApellidoMiembro = inscripcion.Miembro?.Apellido,
                DNI = inscripcion.Miembro?.DNI,
                NombreClase = inscripcion.HorarioClase?.Clase?.Nombre,
                DiaSemana = inscripcion.HorarioClase?.DiaSemana.ToString(),
                HoraInicio = inscripcion.HorarioClase?.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = inscripcion.HorarioClase?.HoraFin.ToString(@"hh\:mm"),
                NumeroSala = inscripcion.HorarioClase?.Sala?.Numero,
                NombreEntrenador = inscripcion.HorarioClase?.Clase?.Entrenador != null
                    ? inscripcion.HorarioClase.Clase.Entrenador.Nombre + " " + inscripcion.HorarioClase.Clase.Entrenador.Apellido
                    : "Sin entrenador",
                FechaInscripcion = inscripcion.FechaInscripcion,
                Estado = inscripcion.Estado ? "Inscripto" : "No Inscripto"
            };

            ViewBag.Dni = dni; // 🔹 Guardamos el DNI para volver al listado

            return View(viewModel);
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

        public IActionResult Inscribirse(int id, string dni)
        {
            var miembro = _context.Miembros.FirstOrDefault(m => m.DNI == dni);
            if (miembro == null) return NotFound();

            var inscripcion = _context.InscripcionClase
                .FirstOrDefault(i => i.HorarioClaseId == id && i.MiembroId == miembro.Id);

            if (inscripcion == null)
            {
                _context.InscripcionClase.Add(new InscripcionClase
                {
                    HorarioClaseId = id,
                    MiembroId = miembro.Id,
                    Estado = true,
                    FechaInscripcion = DateTime.Now
                });
                _context.SaveChanges();
                TempData["Mensaje"] = "Inscripto correctamente.";
            }

            return RedirectToAction(nameof(Index), new { dni = dni });
        }

        public IActionResult Desinscribirse(int id, string dni)
        {
            var miembro = _context.Miembros.FirstOrDefault(m => m.DNI == dni);
            if (miembro == null) return NotFound();

            var inscripcion = _context.InscripcionClase
                .FirstOrDefault(i => i.HorarioClaseId == id && i.MiembroId == miembro.Id);

            if (inscripcion != null)
            {
                _context.InscripcionClase.Remove(inscripcion);
                _context.SaveChanges();
                TempData["Mensaje"] = "Inscripción cancelada.";
            }

            return RedirectToAction(nameof(Index), new { dni = dni });
        }


        // =====================================================
        // GET: InscripcionClases/Delete/5
        // =====================================================
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inscripcion = _context.InscripcionClase
                .Include(i => i.Miembro)
                .Include(i => i.HorarioClase)
                    .ThenInclude(h => h.Clase)
                .FirstOrDefault(i => i.Id == id);

            if (inscripcion == null)
            {
                return NotFound();
            }

            return View(inscripcion);
        }

        // =====================================================
        // POST: InscripcionClases/DeleteConfirmed/5
        // =====================================================
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var inscripcion = _context.InscripcionClase.Find(id);
            if (inscripcion == null)
            {
                return NotFound();
            }

            _context.InscripcionClase.Remove(inscripcion);
            _context.SaveChanges();

            TempData["Success"] = "La inscripción fue eliminada correctamente.";
            return RedirectToAction(nameof(Index));
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