using System;

namespace GimnasioCuerpoSano.Models.ViewModels
{
    public class InscripcionClaseViewModel
    {
        public int Id { get; set; }
        public string NombreMiembro { get; set; } = string.Empty;
        public string ClaseNombre { get; set; } = string.Empty;
        public string EntrenadorNombre { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }  
        public TimeSpan HoraFin { get; set; }
        public string Estado { get; set; } = string.Empty; // "Inscripto" / "No Inscripto"
        public DateTime FechaInscripcion { get; set; }
    }
}

