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
            return View();
        }

        // POST: Miembros/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Miembro miembro)
        {
            if (ModelState.IsValid)
            {
                // Aquí, más adelante, guardaríamos el miembro en la base de datos
                TempData["Mensaje"] = "Miembro dado de alta correctamente";
                return RedirectToAction("Create"); // o a una vista de éxito
            }
            return View(miembro); // Si hay errores, se vuelve a mostrar el formulario
        }

    }
}
