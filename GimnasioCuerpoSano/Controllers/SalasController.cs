using Microsoft.AspNetCore.Mvc;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;

namespace GimnasioCuerpoSano.Controllers
{
    public class SalasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salas
        public IActionResult Index()
        {
            var salas = _context.Salas.ToList();
            return View(salas);
        }

        // GET: Salas/Details/5
        public IActionResult Details(int id)
        {
            var sala = _context.Salas.FirstOrDefault(s => s.ID_Sala == id);
            if (sala == null) return NotFound();
            return View(sala);
        }

        // GET: Salas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Salas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Sala sala)
        {
            if (_context.Salas.Any(s => s.ID_Sala == sala.ID_Sala))
            {
                ModelState.AddModelError("ID_Sala", "Ya existe una sala con este ID.");
            }

            if (!ModelState.IsValid) return View(sala);

            _context.Salas.Add(sala);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: Salas/Edit/5
        public IActionResult Edit(int id)
        {
            var sala = _context.Salas.Find(id);
            if (sala == null) return NotFound();
            return View(sala);
        }

        // POST: Salas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Sala sala)
        {
            if (id != sala.ID_Sala) return NotFound();
            if (!ModelState.IsValid) return View(sala);

            var salaExistente = _context.Salas.Find(id);
            if (salaExistente == null) return NotFound();

            salaExistente.Numero = sala.Numero;
            salaExistente.Nombre = sala.Nombre;
            salaExistente.CapacidadMaxima = sala.CapacidadMaxima;
            salaExistente.Ubicacion = sala.Ubicacion;
            salaExistente.TipoSala = sala.TipoSala;
            salaExistente.Disponible = sala.Disponible;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: Salas/Delete/5
        public IActionResult Delete(int id)
        {
            var sala = _context.Salas.Find(id);
            if (sala == null) return NotFound();
            return View(sala);
        }

        // POST: Salas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var sala = _context.Salas.Find(id);
            if (sala != null)
            {
                _context.Salas.Remove(sala);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        //Generar PDF de listado de salas
        public IActionResult ListadoPDF()
        {
            var salas = _context.Salas.ToList();

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
                            col.Item().AlignCenter().Text("Listado de Salas").Bold().FontSize(20);
                            col.Item().AlignCenter().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            //columns.ConstantColumn(40); // ID
                            columns.RelativeColumn();   // Número
                            columns.RelativeColumn();   // Nombre
                            columns.RelativeColumn();   // Capacidad
                            columns.RelativeColumn();   // Ubicación
                            columns.RelativeColumn();   // Tipo
                            columns.RelativeColumn();   // Disponible
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            //header.Cell().Element(CellHeader).Text("ID");
                            header.Cell().Element(CellHeader).Text("Número");
                            header.Cell().Element(CellHeader).Text("Nombre");
                            header.Cell().Element(CellHeader).Text("Capacidad");
                            header.Cell().Element(CellHeader).Text("Ubicación");
                            header.Cell().Element(CellHeader).Text("Tipo");
                            header.Cell().Element(CellHeader).Text("Disponible");
                        });

                        // Filas
                        foreach (var s in salas)
                        {
                            //table.Cell().Element(CellBody).Text(s.ID_Sala.ToString());
                            table.Cell().Element(CellBody).Text(s.Numero);
                            table.Cell().Element(CellBody).Text(s.Nombre);
                            table.Cell().Element(CellBody).Text(s.CapacidadMaxima.ToString());
                            table.Cell().Element(CellBody).Text(s.Ubicacion);
                            table.Cell().Element(CellBody).Text(s.TipoSala);
                            table.Cell().Element(CellBody).Text(s.Disponible ? "Sí" : "No");
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

            Response.Headers.Add("Content-Disposition", "inline; filename=Listado_Salas.pdf");
            return File(stream, "application/pdf");
        }
    }
}


