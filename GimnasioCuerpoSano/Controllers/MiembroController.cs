using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GimnasioCuerpoSano.Controllers
{
    public class MiembrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MiembrosController(ApplicationDbContext context)
        {
            _context = context;
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
        public IActionResult Details(int id)
        {
            var miembro = _context.Miembros
                                  .Include(m => m.Membresia)
                                  .FirstOrDefault(m => m.Id == id);

            if (miembro == null)
                return NotFound();

            return View(miembro);
        }

        // =====================================================
        // CREAR (GET)
        // =====================================================
        public IActionResult Create()
        {
            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
            return View();
        }

        // =====================================================
        // CREAR (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Miembro miembro)
        {
            if (ModelState.IsValid)
            {
                var membresia = _context.Membresias.Find(miembro.MembresiaId);

                if (membresia == null)
                {
                    ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
                    return View(miembro);
                }

                decimal valorBase = membresia.Precio;

                if (miembro.DescuentoEspecial)
                    valorBase *= 0.85m; // 15% de descuento

                miembro.ValorMembresia = valorBase;
                miembro.FechaAlta = DateTime.Now;

                _context.Miembros.Add(miembro);
                _context.SaveChanges();

                TempData["Mensaje"] = $"Miembro dado de alta correctamente. Valor final: ${miembro.ValorMembresia:N2}";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre");
            return View(miembro);
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

        // =====================================================
        // EDITAR (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Miembro miembro)
        {
            if (id != miembro.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var membresia = _context.Membresias.Find(miembro.MembresiaId);

                if (membresia == null)
                {
                    ModelState.AddModelError("MembresiaId", "Debe seleccionar una membresía válida.");
                    ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
                    return View(miembro);
                }

                decimal valorBase = membresia.Precio;
                if (miembro.DescuentoEspecial)
                    valorBase *= 0.85m;

                miembro.ValorMembresia = valorBase;

                try
                {
                    _context.Update(miembro);
                    _context.SaveChanges();
                    TempData["Mensaje"] = $"Miembro actualizado correctamente. Valor final: ${miembro.ValorMembresia:N2}";
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

            ViewBag.Membresias = new SelectList(_context.Membresias, "Id", "Nombre", miembro.MembresiaId);
            return View(miembro);
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
    }
}

