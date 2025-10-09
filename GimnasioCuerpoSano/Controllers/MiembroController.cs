using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;

namespace GimnasioDesdeCero.Controllers
{
    public class MiembrosController : Controller
    {
        // GET: Miembros/Create
        public IActionResult Create()
        {
            var miembro = new Miembro
            {
                FechaAlta = DateTime.Now
            };
            return View(miembro);
        }

        // GET: Miembros
        public IActionResult Index()
        {
            var miembros = TempData["Miembros"] as List<Miembro> ?? new List<Miembro>();
            // Guardamos nuevamente la lista para mantenerla viva después del redirect
            TempData.Keep("Miembros");
            return View(miembros);
        }

        // GET: Miembros/Details/5
        public IActionResult Details(int id)
        {
            var miembros = TempData["Miembros"] as List<Miembro> ?? new List<Miembro>();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            var miembro = miembros[id];
            TempData.Keep("Miembros");
            return View(miembro);
        }

        // GET: Miembros/Edit/5
        public IActionResult Edit(int id)
        {
            var miembros = TempData["Miembros"] as List<Miembro> ?? new List<Miembro>();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            var miembro = miembros[id];
            TempData.Keep("Miembros");
            return View(miembro);
        }

        // POST: Miembros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Miembro miembro)
        {
            if (ModelState.IsValid)
            {
                decimal valorBase = miembro.TipoMembresia switch
                {
                    "Mensual" => 35000,
                    "Trimestral" => 90000,
                    "Anual" => 390000,
                    _ => 0
                };

                if (miembro.DescuentoEspecial)
                    valorBase *= 0.85m;

                miembro.ValorMembresia = valorBase;
                miembro.FechaAlta = DateTime.Now;

                var miembros = TempData["Miembros"] as List<Miembro> ?? new List<Miembro>();
                miembros.Add(miembro);

                TempData["Miembros"] = miembros;
                TempData["Mensaje"] = $"Miembro dado de alta correctamente. Valor final: ${miembro.ValorMembresia}";

                return RedirectToAction("Index");
            }

            return View(miembro);
        }

        // POST: Miembros/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Miembro miembro)
        {
            if (!ModelState.IsValid)
                return View(miembro);

            var miembros = TempData["Miembros"] as List<Miembro> ?? new List<Miembro>();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            miembros[id] = miembro;
            TempData["Miembros"] = miembros;
            TempData["Mensaje"] = "Miembro modificado correctamente.";

            return RedirectToAction("Index");
        }
    }
}

