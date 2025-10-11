using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GimnasioCuerpoSano.Controllers
{
    public class MiembrosController : Controller
    {
        // ✅ Método auxiliar: leer lista desde TempData
        private List<Miembro> ObtenerMiembros()
        {
            if (TempData.ContainsKey("Miembros") && TempData["Miembros"] is string miembrosJson)
            {
                var lista = JsonSerializer.Deserialize<List<Miembro>>(miembrosJson);
                if (lista != null)
                {
                    TempData.Keep("Miembros"); // conservar datos tras redirect
                    return lista;
                }
            }
            return new List<Miembro>();
        }

        // ✅ Método auxiliar: guardar lista en TempData
        private void GuardarMiembros(List<Miembro> miembros)
        {
            TempData["Miembros"] = JsonSerializer.Serialize(miembros);
        }

        // GET: Miembros
        public IActionResult Index()
        {
            var miembros = ObtenerMiembros();
            return View(miembros);
        }

        // GET: Miembros/Create
        public IActionResult Create()
        {
            var miembro = new Miembro
            {
                FechaAlta = DateTime.Now
            };
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

                var miembros = ObtenerMiembros();
                miembros.Add(miembro);
                GuardarMiembros(miembros);

                TempData["Mensaje"] = $"Miembro dado de alta correctamente. Valor final: ${miembro.ValorMembresia}";
                return RedirectToAction(nameof(Index));
            }
            return View(miembro);
        }

        // GET: Miembros/Details/{id}
        public IActionResult Details(int id)
        {
            var miembros = ObtenerMiembros();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            var miembro = miembros[id];
            ViewData["Index"] = id;
            return View(miembro);
        }

        // GET: Miembros/Edit/{id}
        public IActionResult Edit(int id)
        {
            var miembros = ObtenerMiembros();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            var miembro = miembros[id];
            ViewData["Index"] = id;
            return View(miembro);
        }

        // POST: Miembros/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Miembro miembro)
        {
            if (!ModelState.IsValid)
                return View(miembro);

            var miembros = ObtenerMiembros();
            if (id < 0 || id >= miembros.Count)
                return NotFound();

            miembros[id] = miembro;
            GuardarMiembros(miembros);

            TempData["Mensaje"] = "Miembro modificado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
