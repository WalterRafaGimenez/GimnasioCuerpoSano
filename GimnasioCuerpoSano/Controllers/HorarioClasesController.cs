using QuestPDF.Fluent; // Necesario
using QuestPDF.Helpers; // Necesario
using QuestPDF.Infrastructure;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System; // Necesario para TimeSpan
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;
using System.Text.Json; // Usaremos el serializador nativo de .NET 8 (System.Text.Json)
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    public class HorarioClasesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HorarioClasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: HorarioClases/Create
        // =====================================================



        public IActionResult Create()
        {
            ViewData["Title"] = "Crear Horario de Clase";

            // Clases y Salas
            ViewBag.Clases = new SelectList(_context.Clase.OrderBy(c => c.Nombre), "Id", "Nombre");
            ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Nombre), "ID_Sala", "Nombre");

            // Enum DiasSemana
            ViewBag.DiasSemana = new SelectList(
                Enum.GetValues(typeof(DiaSemana))
                    .Cast<DiaSemana>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = d.GetType()
                                .GetMember(d.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString()
                    }),
                "Value", "Text"
            );

            // JS para hora fin
            ViewBag.ClasesJsonString = JsonConvert.SerializeObject(
                _context.Clase.Select(c => new { c.Id, c.DuracionMinutos }).ToList()
            );

            return View();
        }


        // POST: HorarioClases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClaseId,SalaId,DiaSemana,HoraInicio,HoraFin,CapacidadMaxima,SePuedeInscribir")] HorarioClase horario)
        {
            ViewData["Title"] = "Crear Nuevo Horario de Clase";

            // Validación extra para DiaSemana (opción por defecto 0)
            if ((int)horario.DiaSemana == 0)
            {
                ModelState.AddModelError("DiaSemana", "Debe seleccionar un día válido.");
            }

            // Validación básica del modelo
            if (!ModelState.IsValid)
            {
                // Depuración en consola
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Error en '{key}': {error.ErrorMessage}");
                    }
                }

                // Recargar SelectLists
                ViewBag.Clases = new SelectList(_context.Clase.OrderBy(c => c.Nombre), "Id", "Nombre", horario.ClaseId);
                ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Numero", horario.SalaId);

                // Días de la semana con DisplayAttribute
                ViewBag.DiasSemana = Enum.GetValues(typeof(DiaSemana))
                    .Cast<DiaSemana>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = d.GetType().GetMember(d.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString(),
                        Selected = d == horario.DiaSemana
                    })
                    .ToList();

                // JS
                ViewBag.ClasesJsonString = JsonConvert.SerializeObject(
                    _context.Clase.Select(c => new { c.Id, c.DuracionMinutos }).ToList()
                );

                return View(horario);
            }

            // Buscar entidades necesarias
            var clase = await _context.Clase.FindAsync(horario.ClaseId);
            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.ID_Sala == horario.SalaId);

            if (clase == null || sala == null)
            {
                ModelState.AddModelError("", "Clase o Sala no válida. Verifique las selecciones.");

                // Recargar SelectLists como arriba
                ViewBag.Clases = new SelectList(_context.Clase.OrderBy(c => c.Nombre), "Id", "Nombre", horario.ClaseId);
                ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Numero", horario.SalaId);
                ViewBag.DiasSemana = Enum.GetValues(typeof(DiaSemana))
                    .Cast<DiaSemana>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = d.GetType().GetMember(d.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString(),
                        Selected = d == horario.DiaSemana
                    })
                    .ToList();
                ViewBag.ClasesJsonString = JsonConvert.SerializeObject(
                    _context.Clase.Select(c => new { c.Id, c.DuracionMinutos }).ToList()
                );

                return View(horario);
            }

            // Validación de capacidad
            if (horario.CapacidadMaxima > sala.CapacidadMaxima)
            {
                ModelState.AddModelError("CapacidadMaxima", $"La capacidad máxima ({horario.CapacidadMaxima}) no puede superar la capacidad física de la sala ({sala.CapacidadMaxima}).");

                // Recargar SelectLists igual que antes
                ViewBag.Clases = new SelectList(_context.Clase.OrderBy(c => c.Nombre), "Id", "Nombre", horario.ClaseId);
                ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Numero", horario.SalaId);
                ViewBag.DiasSemana = Enum.GetValues(typeof(DiaSemana))
                    .Cast<DiaSemana>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = d.GetType().GetMember(d.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString(),
                        Selected = d == horario.DiaSemana
                    })
                    .ToList();
                ViewBag.ClasesJsonString = JsonConvert.SerializeObject(
                    _context.Clase.Select(c => new { c.Id, c.DuracionMinutos }).ToList()
                );

                return View(horario);
            }

            // Calcular HoraFin
            horario.HoraFin = horario.HoraInicio.Add(TimeSpan.FromMinutes(clase.DuracionMinutos));

            // Guardar
            try
            {
                _context.Add(horario);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear el horario (DB o Lógica): {ex.InnerException?.Message ?? ex.Message}");

                // Recargar SelectLists como arriba
                ViewBag.Clases = new SelectList(_context.Clase.OrderBy(c => c.Nombre), "Id", "Nombre", horario.ClaseId);
                ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Numero", horario.SalaId);
                ViewBag.DiasSemana = Enum.GetValues(typeof(DiaSemana))
                    .Cast<DiaSemana>()
                    .Select(d => new SelectListItem
                    {
                        Value = ((int)d).ToString(),
                        Text = d.GetType().GetMember(d.ToString())
                                .First()
                                .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString(),
                        Selected = d == horario.DiaSemana
                    })
                    .ToList();
                ViewBag.ClasesJsonString = JsonConvert.SerializeObject(
                    _context.Clase.Select(c => new { c.Id, c.DuracionMinutos }).ToList()
                );

                return View(horario);
            }

            return RedirectToAction(nameof(Index));
        }

        // Asegurate de que tu [HttpGet] Create también llame a CargarViewBags:
        /*
        public IActionResult Create()
        {
            CargarViewBags(null);
            return View();
        }
        */

        // =====================================================
        // GET: HorarioClases/Edit/5 (Aseguramos que el Include traiga la Sala para la validación)
        // =====================================================
        // HorarioClasesController.cs (Método [HttpGet] Edit)

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // 1. Cargar el horario, incluyendo la Clase y Sala para acceso inmediato
            var horarioClase = await _context.HorariosClase
                .Include(h => h.Clase) // Para acceder a DuracionMinutos si es necesario
                .Include(h => h.Sala)  // Por si lo necesitas en futuras validaciones
                .FirstOrDefaultAsync(m => m.Id == id);

            if (horarioClase == null) return NotFound();

            // 2. Llama a la función auxiliar que rellena TODOS los ViewBags
            // (Incluyendo Clase, Sala, DiasSemana y, CRUCIALMENTE, ClasesJsonString)
            CargarViewBags(horarioClase);

            return View(horarioClase);
        }
        // =====================================================
        // POST: HorarioClases/Edit/5 (Añadimos Validación de Capacidad)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,SalaId,DiaSemana,HoraInicio,HoraFin,CapacidadMaxima,SePuedeInscribir")] HorarioClase horario)
        {
            if (id != horario.Id) return NotFound();

            ViewData["Title"] = "Editar Horario de Clase";

            // 1. VALIDACIÓN DEL MODELO BÁSICA
            if (!ModelState.IsValid)
            {
                // Si la validación falla (ej. campos requeridos), recarga la vista y debe mostrar el error.
                CargarViewBags(horario);
                return View(horario);
            }

            // 2. BUSCAR ENTIDADES NECESARIAS
            var clase = await _context.Clase.FindAsync(horario.ClaseId);
            // Usamos FirstOrDefaultAsync para evitar problemas con FindAsync y la PK de Sala.
            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.ID_Sala == horario.SalaId);

            if (clase == null)
            {
                ModelState.AddModelError("ClaseId", "La Clase seleccionada no es válida.");
                CargarViewBags(horario);
                return View(horario);
            }

            if (sala == null)
            {
                ModelState.AddModelError("SalaId", "La Sala seleccionada no es válida.");
                CargarViewBags(horario);
                return View(horario);
            }

            // 3. VALIDACIÓN DE LÓGICA DE NEGOCIO (Capacidad de la Sala)
            if (horario.CapacidadMaxima > sala.CapacidadMaxima)
            {
                ModelState.AddModelError("CapacidadMaxima", $"La capacidad máxima ({horario.CapacidadMaxima}) no puede superar la capacidad física de la sala ({sala.CapacidadMaxima}).");
                CargarViewBags(horario);
                return View(horario);
            }

            // 4. CÁLCULO DE HORA FIN (Backend)
            horario.HoraFin = horario.HoraInicio.Add(TimeSpan.FromMinutes(clase.DuracionMinutos));

            // 5. MARCAR ENTIDAD COMO MODIFICADA Y GUARDAR (SOLUCIÓN DE EF CORE)
            try
            {
                // Esta es la técnica más confiable para actualizar un objeto desconectado.
                _context.Attach(horario).State = EntityState.Modified;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.HorariosClase.Any(e => e.Id == horario.Id))
                    return NotFound();

                throw;
            }
            // CAPTURADOR DE ERRORES GENERAL: Captura cualquier fallo de la DB.
            catch (Exception ex)
            {
                // Este mensaje aparecerá en rojo en el resumen de validación de tu vista.
                ModelState.AddModelError("", $"Error al guardar los cambios (DB o Lógica): {ex.InnerException?.Message ?? ex.Message}");
                CargarViewBags(horario);
                return View(horario);
            }

            return RedirectToAction(nameof(Index));
        }

        // ... (El resto de métodos Index, Details, Delete, ToggleInscripciones se mantienen iguales)

        // =====================================================
        // MÉTODO AUXILIAR: Cargar TODOS los ViewBags (Actualizado)
        // =====================================================
        private void CargarViewBags(HorarioClase? horario)
        {
            // 1. Clases para dropdown y JSON
            var clases = _context.Clase.OrderBy(c => c.Nombre).ToList();
            ViewBag.Clases = new SelectList(clases, "Id", "Nombre", horario?.ClaseId);

            // JSON para JS (hora fin automática)
            var clasesParaJson = clases
                .Select(c => new { c.Id, c.DuracionMinutos })
                .ToList();
            ViewBag.ClasesJsonString = System.Text.Json.JsonSerializer.Serialize(clasesParaJson);

            // 2. Salas (mostramos el nombre, no el número)
            ViewBag.Salas = new SelectList(_context.Salas.OrderBy(s => s.Numero), "ID_Sala", "Nombre", horario?.SalaId);

            // 3. Días de la semana usando DisplayAttribute
            var diasSemana = Enum.GetValues(typeof(DiaSemana))
                .Cast<DiaSemana>()
                .Select(d => new SelectListItem
                {
                    Value = ((int)d).ToString(),
                    Text = d.GetType()
                            .GetMember(d.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?.Name ?? d.ToString()
                })
                .ToList();

            ViewBag.DiasSemana = new SelectList(diasSemana, "Value", "Text", horario?.DiaSemana);
        }


        // =====================================================
        // El resto de métodos (Index, Details, Delete, ToggleInscripciones) van aquí
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var horarios = await _context.HorariosClase
                .Include(h => h.Clase)
                .Include(h => h.Sala)
                .ToListAsync();

            return View(horarios);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var horario = await _context.HorariosClase
                .Include(h => h.Clase)
                .Include(h => h.Sala)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (horario == null) return NotFound();

            return View(horario);
        }

        // GET: HorarioClases/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var horario = await _context.HorariosClase
                .Include(h => h.Clase)
                .Include(h => h.Sala)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (horario == null) return NotFound();

            return View(horario);
        }

        // POST: HorarioClases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var horario = await _context.HorariosClase.FindAsync(id);
            if (horario != null)
            {
                _context.HorariosClase.Remove(horario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleInscripciones(int id)
        {
            var horario = await _context.HorariosClase.FindAsync(id);
            if (horario == null) return NotFound();

            horario.SePuedeInscribir = !horario.SePuedeInscribir;
            _context.Update(horario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GET: HorarioClases/ImprimirListado
        // =====================================================
        public async Task<IActionResult> ImprimirListado()
        {
            // 1. Obtener los datos (sin el ordenamiento problemático para el SQL Server)
            var horarios = await _context.HorariosClase
                .Include(h => h.Clase)
                .Include(h => h.Sala)
                .ToListAsync(); // <--- La consulta SQL se ejecuta aquí

            // 2. Ordenar los datos en memoria (C#) donde el Enum es entendido
            horarios = horarios
                .OrderBy(h => (int)h.DiaSemana) // Ordena por el valor INT del Enum
                .ThenBy(h => h.HoraInicio)      // Luego por hora de inicio
                .ToList();

            // 3. Configuración del Logo (usa la misma lógica de Miembros)
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "Logo.png");
            byte[]? logoBytes = System.IO.File.Exists(logoPath)
                ? System.IO.File.ReadAllBytes(logoPath)
                : null;

            QuestPDF.Settings.License = LicenseType.Community;

            // 4. Generación del PDF con QuestPDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.PageColor(Colors.White);

                    // ============= CABECERA =============
                    page.Header().Column(header =>
                    {
                        if (logoBytes != null)
                        {
                            header.Item().AlignCenter().Container().Width(100).Image(logoBytes);
                        }

                        header.Item().PaddingTop(5).Text("Gimnasio Cuerpo Sano")
                            .FontSize(18).Bold().FontColor(Colors.Blue.Medium)
                            .AlignCenter();

                        header.Item().PaddingTop(5).Text("Listado de Horarios de Clases")
                            .FontSize(14).Bold().FontColor(Colors.Grey.Darken1)
                            .AlignCenter();
                    });

                    // ============= CONTENIDO: TABLA =============
                    page.Content().PaddingVertical(15).Table(table =>
                    {
                        // Definición de Columnas (ajustar el RelativeColumn para el ancho)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Clase / Sala
                            columns.RelativeColumn(1.2f); // Día
                            columns.RelativeColumn(1.8f); // Horario (Hora Inicio - Hora Fin)
                            columns.RelativeColumn(1.2f); // Capacidad
                            columns.RelativeColumn(1.5f); // Inscripciones
                        });

                        // Encabezado de la Tabla
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Clase / Sala").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Día").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Horario").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Capacidad Máxima").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Inscripciones").Bold();
                        });

                        // Filas de Datos
                        foreach (var h in horarios)
                        {
                            // Clase / Sala
                            table.Cell().Padding(5).Text($"{h.Clase?.Nombre ?? "N/A"}\nSala: {h.Sala?.Nombre ?? "N/A"}").LineHeight(1.5f);

                            // Día
                            table.Cell().Padding(5).Text(h.DiaSemana.ToString()); // Usará el nombre del Enum (Lunes, Martes, etc.)

                            // Horario
                            table.Cell().Padding(5).Text($"{h.HoraInicio.ToString(@"hh\:mm")} - {h.HoraFin.ToString(@"hh\:mm")}\n({h.Clase?.DuracionMinutos ?? 0} min)").LineHeight(1.5f);

                            // Capacidad Máxima
                            table.Cell().Padding(5).Text(h.CapacidadMaxima.ToString()).AlignCenter();

                            // Se Puede Inscribir
                            table.Cell().Padding(5).Text(h.SePuedeInscribir ? "Sí" : "No").FontColor(h.SePuedeInscribir ? Colors.Green.Darken2 : Colors.Red.Darken2).Bold().AlignCenter();
                        }
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

            // 5. Devolver el archivo PDF para abrir en una pestaña aparte
            Response.Headers.Add("Content-Disposition", "inline; filename=ListadoHorariosClases.pdf");
            return File(pdfBytes, "application/pdf");
        }
    }
}
