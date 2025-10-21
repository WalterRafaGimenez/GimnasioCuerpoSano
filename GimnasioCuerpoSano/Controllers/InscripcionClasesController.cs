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
                    Estado = _context.InscripcionesClase
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
                var inscripcion = _context.InscripcionesClase
                    .FirstOrDefault(ic => ic.MiembroId == miembroId && ic.HorarioClaseId == i.Id);

                if (i.Estado == "Inscripto" && inscripcion == null)
                {
                    _context.InscripcionesClase.Add(new InscripcionClase
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

        //IMPRESION A PDF
        public IActionResult ListadoPDF()
        {
            var inscripciones = _context.HorariosClase

                .Select(h => new InscripcionClaseViewModel
                {
                    ClaseNombre = h.Clase.Nombre,
                    EntrenadorNombre = h.Clase.Entrenador.Nombre + " " + h.Clase.Entrenador.Apellido,
                    HoraInicio = h.HoraInicio,
                    HoraFin = h.HoraFin
                }).ToList();

            var stream = new MemoryStream();
            var rutaLogo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Logo.png");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(80).Image(rutaLogo);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignCenter().Text("Listado de Clases").Bold().FontSize(20);
                            col.Item().AlignCenter().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        });
                    });

                    page.Content().Table(table =>
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

                        static IContainer CellHeader(IContainer container) =>
                            container.Padding(5).Background(Colors.Grey.Lighten2).Border(1).AlignCenter();

                        static IContainer CellBody(IContainer container) =>
                            container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).AlignCenter();
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Página ").FontSize(10);
                        txt.CurrentPageNumber().FontSize(10);
                        txt.Span(" de ").FontSize(10);
                        txt.TotalPages().FontSize(10);
                    });
                });
            });

            document.GeneratePdf(stream);
            stream.Position = 0;

            Response.Headers.Add("Content-Disposition", "inline; filename=Listado_Clases.pdf");
            return File(stream, "application/pdf");
        }

    }
}
