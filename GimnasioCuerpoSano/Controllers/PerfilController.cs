using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GimnasioCuerpoSano.Data;
using System.Threading.Tasks;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Miembro")]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PerfilController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PERFIL - Solo ve sus propios datos
        // =====================================================
        public async Task<IActionResult> Index()
        {
            // Tomamos el Email del usuario logueado
            var emailUsuario = User.Identity?.Name;

            if (string.IsNullOrEmpty(emailUsuario))
            {
                return RedirectToAction("Login", "Account");
            }

            // Buscamos el miembro vinculado a ese mail
            var miembro = await _context.Miembros
                .Include(m => m.Membresia)
                .FirstOrDefaultAsync(m => m.Mail == emailUsuario);

            if (miembro == null)
            {
                TempData["Error"] = "No se encontró la información del miembro asociado a este usuario.";
                return RedirectToAction("Index", "Home");
            }

            return View(miembro);
        }
    }
}


