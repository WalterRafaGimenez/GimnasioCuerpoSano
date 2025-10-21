using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class HorarioClase
    {
        public int Id { get; set; }

        [Required]
        public int ClaseId { get; set; }
        public virtual Clase? Clase { get; set; }

        [Required]
        [StringLength(20)]
        public string? DiaSemana { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        [Required]
        public int CapacidadMaxima { get; set; }

        [Required]
        public bool SePuedeInscribir { get; set; } = true;

        // Relación con InscripcionClase
        public virtual ICollection<InscripcionClase> Inscripciones { get; set; } = new List<InscripcionClase>();
    }
}

