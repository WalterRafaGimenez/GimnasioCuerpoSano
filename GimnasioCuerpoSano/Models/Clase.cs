using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Clase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Nombre { get; set; }

        [Required]
        public decimal Precio { get; set; }

        [Required]
        public int DuracionMinutos { get; set; }

        // Relación con el entrenador
        public int EntrenadorId { get; set; }
        public Entrenador? Entrenador { get; set; }

        // Relación con HorarioClase
        public virtual ICollection<HorarioClase> Horarios { get; set; } = new List<HorarioClase>();

    }
}

