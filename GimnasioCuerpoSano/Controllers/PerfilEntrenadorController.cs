using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Entrenador")]
    public class PerfilEntrenadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PerfilEntrenadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Obtenemos el correo del usuario logueado
            var email = User.Identity.Name;

            // Buscamos al entrenador en la base de datos
            var entrenador = await _context.Entrenadores
                .Include(e => e.Clases)
                    .ThenInclude(c => c.Sala)
                .FirstOrDefaultAsync(e => e.Email == email);

            if (entrenador == null)
            {
                return NotFound("No se encontró el entrenador logueado.");
            }

            return View(entrenador);
        }
    }
}
