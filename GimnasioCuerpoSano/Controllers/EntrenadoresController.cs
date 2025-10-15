using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Http;

namespace GimnasioCuerpoSano.Controllers
{
    public class EntrenadoresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntrenadoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Entrenadores
        public IActionResult Index()
        {
            var entrenadores = _context.Entrenadores.ToList();
            return View(entrenadores);
        }

        // GET: Entrenadores/Details/5
        public IActionResult Details(int id)
        {
            var entrenador = _context.Entrenadores.FirstOrDefault(e => e.Id == id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // GET: Entrenadores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Entrenadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Entrenador entrenador, IFormFile? certificado)
        {
            try
            {
                if (!ModelState.IsValid) return View(entrenador);

                // Carpeta segura fuera de wwwroot
                var uploadsFolder = Path.Combine("C:\\Temp", "certificados");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                if (certificado != null && certificado.Length > 0)
                {
                    var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(certificado.FileName);
                    var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                    using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        certificado.CopyTo(fileStream);
                    }

                    entrenador.RutaCertificado = rutaCompleta;
                }

                _context.Entrenadores.Add(entrenador);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al guardar el entrenador: " + ex.Message;
                return View(entrenador);
            }
        }

        // GET: Entrenadores/Edit/5
        public IActionResult Edit(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // POST: Entrenadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Entrenador entrenador, IFormFile? certificado)
        {
            if (id != entrenador.Id) return NotFound();

            if (!ModelState.IsValid) return View(entrenador);

            var entrenadorExistente = _context.Entrenadores.Find(id);
            if (entrenadorExistente == null) return NotFound();

            // Actualizar campos
            entrenadorExistente.Nombre = entrenador.Nombre;
            entrenadorExistente.Apellido = entrenador.Apellido;
            entrenadorExistente.Direccion = entrenador.Direccion;
            entrenadorExistente.Telefono = entrenador.Telefono;
            entrenadorExistente.Email = entrenador.Email;
            entrenadorExistente.Especialidad = entrenador.Especialidad;
            entrenadorExistente.FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado;

            if (certificado != null && certificado.Length > 0)
            {
                var uploadsFolder = Path.Combine("C:\\Temp", "certificados");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(certificado.FileName);
                var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    certificado.CopyTo(fileStream);
                }

                entrenadorExistente.RutaCertificado = rutaCompleta;
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: Entrenadores/Delete/5
        public IActionResult Delete(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // POST: Entrenadores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(entrenador.RutaCertificado) && System.IO.File.Exists(entrenador.RutaCertificado))
                        System.IO.File.Delete(entrenador.RutaCertificado);
                }
                catch { }

                _context.Entrenadores.Remove(entrenador);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}



