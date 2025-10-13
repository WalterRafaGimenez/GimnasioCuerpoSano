using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")] //Para los roles
    
    public class MembresiasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembresiasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Membresias
        public async Task<IActionResult> Index()
        {
            return View(await _context.Membresias.ToListAsync());
        }

        // GET: Membresias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membresia == null)
            {
                return NotFound();
            }

            return View(membresia);
        }

        // GET: Membresias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Membresias/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Precio")] Membresia membresia)
        {
            if (ModelState.IsValid)
            {
                _context.Add(membresia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(membresia);
        }

        // GET: Membresias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia == null)
            {
                return NotFound();
            }
            return View(membresia);
        }

        // POST: Membresias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Precio")] Membresia membresia)
        {
            if (id != membresia.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membresia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembresiaExists(membresia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(membresia);
        }

        // GET: Membresias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membresia = await _context.Membresias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (membresia == null)
            {
                return NotFound();
            }

            return View(membresia);
        }

        // POST: Membresias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia != null)
            {
                _context.Membresias.Remove(membresia);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MembresiaExists(int id)
        {
            return _context.Membresias.Any(e => e.Id == id);
        }
    }
}
