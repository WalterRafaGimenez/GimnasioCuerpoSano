using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using GimnasioCuerpoSano.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GimnasioCuerpoSano.Controllers
{
    [Authorize(Roles = "Entrenador")]
    public class ClasesEntrenadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClasesEntrenadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var email = User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Index", "Home");

            // Traemos al entrenador con clases, salas y horarios
            var entrenador = await _context.Entrenadores
                .Include(e => e.Clases)
                    .ThenInclude(c => c.Sala)
                .Include(e => e.Clases)
                    .ThenInclude(c => c.Horarios)
                .FirstOrDefaultAsync(e => e.Email == email);

            if (entrenador == null)
                return RedirectToAction("Index", "Home");

            var clasesViewModel = new List<ClaseEntrenadorViewModel>();

            foreach (var clase in entrenador.Clases)
            {
                if (clase.Horarios != null)
                {
                    foreach (var horario in clase.Horarios)
                    {
                        clasesViewModel.Add(new ClaseEntrenadorViewModel
                        {
                            NombreClase = clase.Nombre,
                            SalaNombre = clase.Sala?.Nombre ?? clase.Sala?.Numero ?? "Sin sala",
                            DiaSemana = horario.DiaSemana.ToString(),
                            HoraInicio = horario.HoraInicio,
                            HoraFin = horario.HoraFin,
                            DuracionMinutos = clase.DuracionMinutos
                        });
                    }
                }
            }

            // Ordenar por día y hora
            clasesViewModel = clasesViewModel
                .OrderBy(c => c.DiaSemana)
                .ThenBy(c => c.HoraInicio)
                .ToList();

            // ViewModel final que espera la vista
            var model = new MisClasesEntrenadorViewModel
            {
                FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado,
                Clases = clasesViewModel
            };

            return View(model);
        }
    }
}
