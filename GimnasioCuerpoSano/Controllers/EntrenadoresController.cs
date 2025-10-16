using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using GimnasioCuerpoSano.Data;
using GimnasioCuerpoSano.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace GimnasioCuerpoSano.Controllers
{
    public class EntrenadoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Para acceder a wwwroot

        public EntrenadoresController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
                if (!ModelState.IsValid)
                {
                    ViewBag.Error = "El modelo no es válido.";
                    return View(entrenador);
                }

                // Carpeta destino dentro de wwwroot/certificados2
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "certificados2");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                if (certificado != null && certificado.Length > 0)
                {
                    var nombreArchivo = Guid.NewGuid() + Path.GetExtension(certificado.FileName);
                    var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        certificado.CopyTo(stream);
                    }

                    // Guardar ruta relativa en la entidad
                    entrenador.RutaCertificado = "/certificados2/" + nombreArchivo;
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

            try
            {
                // Actualizar datos
                entrenadorExistente.Nombre = entrenador.Nombre;
                entrenadorExistente.Apellido = entrenador.Apellido;
                entrenadorExistente.Direccion = entrenador.Direccion;
                entrenadorExistente.Telefono = entrenador.Telefono;
                entrenadorExistente.Email = entrenador.Email;
                entrenadorExistente.Especialidad = entrenador.Especialidad;
                entrenadorExistente.FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado;

                if (certificado != null && certificado.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "certificados2");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var nombreArchivo = Guid.NewGuid() + Path.GetExtension(certificado.FileName);
                    var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        certificado.CopyTo(stream);
                    }

                    // Eliminar certificado anterior si existía
                    if (!string.IsNullOrEmpty(entrenadorExistente.RutaCertificado))
                    {
                        var rutaVieja = Path.Combine(_environment.WebRootPath, entrenadorExistente.RutaCertificado.TrimStart('/'));
                        if (System.IO.File.Exists(rutaVieja))
                            System.IO.File.Delete(rutaVieja);
                    }

                    // Guardar la nueva ruta relativa
                    entrenadorExistente.RutaCertificado = "/certificados2/" + nombreArchivo;
                }

                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al editar el entrenador: " + ex.Message;
                return View(entrenador);
            }
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
                    if (!string.IsNullOrEmpty(entrenador.RutaCertificado))
                    {
                        var rutaArchivo = Path.Combine(_environment.WebRootPath, entrenador.RutaCertificado.TrimStart('/'));
                        if (System.IO.File.Exists(rutaArchivo))
                            System.IO.File.Delete(rutaArchivo);
                    }

                    _context.Entrenadores.Remove(entrenador);
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al eliminar: " + ex.Message;
                    return View(entrenador);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        //prueba para guardar el archivo en c/Temp/certificados
        [HttpGet]
        public IActionResult GuardarArchivo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GuardarArchivo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Mensaje = "No se seleccionó ningún archivo.";
                return View();
            }

            try
            {
                // Guardar dentro del proyecto
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "certificados");

                if (!Directory.Exists(rutaCarpeta))
                {
                    Directory.CreateDirectory(rutaCarpeta);
                }

                var rutaArchivo = Path.Combine(rutaCarpeta, archivo.FileName);

                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                ViewBag.Mensaje = $"Archivo guardado correctamente en {rutaArchivo}";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error al guardar el archivo: {ex.Message}";
            }

            return View();
        }


    }
}




