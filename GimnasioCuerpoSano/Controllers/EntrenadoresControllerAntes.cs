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
        private readonly IWebHostEnvironment _environment; // Permite acceder a wwwroot

        public EntrenadoresController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =======================
        // LISTADO DE ENTRENADORES
        // =======================
        public IActionResult Index()
        {
            var entrenadores = _context.Entrenadores.ToList();
            return View(entrenadores);
        }

        // =======================
        // DETALLES DE UN ENTRENADOR
        // =======================
        public IActionResult Details(int id)
        {
            var entrenador = _context.Entrenadores.FirstOrDefault(e => e.Id == id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        // =======================
        // CREAR NUEVO ENTRENADOR
        // =======================
        public IActionResult Create()
        {
            return View();
        }

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

                // Carpeta dentro de wwwroot para guardar certificados
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "certificados");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Guardar archivo si se adjunta
                if (certificado != null && certificado.Length > 0)
                {
                    try
                    {
                        var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(certificado.FileName);
                        var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            certificado.CopyTo(stream);
                        }

                        // Guardar ruta relativa para usar en la web
                        entrenador.RutaCertificado = "/certificados/" + nombreArchivo;
                    }
                    catch (Exception exFile)
                    {
                        ViewBag.Advertencia = "No se pudo guardar el archivo: " + exFile.Message;
                        entrenador.RutaCertificado = null; // Dejar vacío si falla
                    }
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

        // =======================
        // EDITAR ENTRENADOR
        // =======================
        public IActionResult Edit(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

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
                // Actualizar campos básicos
                entrenadorExistente.Nombre = entrenador.Nombre;
                entrenadorExistente.Apellido = entrenador.Apellido;
                entrenadorExistente.Direccion = entrenador.Direccion;
                entrenadorExistente.Telefono = entrenador.Telefono;
                entrenadorExistente.Email = entrenador.Email;
                entrenadorExistente.Especialidad = entrenador.Especialidad;
                entrenadorExistente.FechaVencimientoCertificado = entrenador.FechaVencimientoCertificado;

                // Guardar nuevo certificado si se adjunta
                if (certificado != null && certificado.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "certificados");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(certificado.FileName);
                    var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                    using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        certificado.CopyTo(fileStream);
                    }

                    // Eliminar certificado anterior si existía
                    if (!string.IsNullOrEmpty(entrenadorExistente.RutaCertificado))
                    {
                        var rutaVieja = Path.Combine(_environment.WebRootPath, entrenadorExistente.RutaCertificado.TrimStart('/'));
                        if (System.IO.File.Exists(rutaVieja))
                            System.IO.File.Delete(rutaVieja);
                    }

                    entrenadorExistente.RutaCertificado = "/certificados/" + nombreArchivo;
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

        // =======================
        // ELIMINAR ENTRENADOR
        // =======================
        public IActionResult Delete(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador == null) return NotFound();
            return View(entrenador);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var entrenador = _context.Entrenadores.Find(id);
            if (entrenador != null)
            {
                try
                {
                    // Borrar archivo de certificado
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
    }
}



