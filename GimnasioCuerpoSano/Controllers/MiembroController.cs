using Microsoft.AspNetCore.Mvc;
using GimnasioCuerpoSano.Models;

namespace GimnasioCuerpoSano.Controllers
{
    public class MiembrosController : Controller
    {
        // Aquí pondremos nuestras acciones

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
                // Valores base según tipo de membresía
                decimal valorBase = miembro.TipoMembresia switch
                {
                    "Mensual" => 35000,
                    "Trimestral" => 90000,
                    "Anual" => 390000,
                    _ => 0
                };

                // Aplicar descuento del 15% si corresponde
                if (miembro.DescuentoEspecial)
                    valorBase *= 0.85m;

                miembro.ValorMembresia = valorBase;
                miembro.FechaAlta = DateTime.Now;

                TempData["Mensaje"] = $"Miembro dado de alta correctamente. Valor final: ${miembro.ValorMembresia}";

                return RedirectToAction("Create");
            }
            return View(miembro);
        }


    }
}
