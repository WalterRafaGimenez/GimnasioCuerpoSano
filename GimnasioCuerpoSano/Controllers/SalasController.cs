using Microsoft.AspNetCore.Mvc;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
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
    }
}

