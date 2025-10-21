using System;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class InscripcionClase
    {
        public int Id { get; set; }

        [Required]
        public int HorarioClaseId { get; set; }
        public virtual HorarioClase? HorarioClase { get; set; }

        [Required]
        public int MiembroId { get; set; }
        public virtual Miembro? Miembro { get; set; }

        [Required]
        public bool Estado { get; set; } = true; // true = inscripto, false = no

        [Required]
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
    }
}
