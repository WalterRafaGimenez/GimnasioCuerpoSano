using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Clase
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la clase es obligatorio.")]
        [StringLength(100)]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(1, 600, ErrorMessage = "La duración debe ser entre 1 y 600 minutos.")]
        public int DuracionMinutos { get; set; }

        // Relación con Entrenador
        [Required(ErrorMessage = "Debe seleccionar un entrenador.")]
        public int EntrenadorId { get; set; }
        public Entrenador? Entrenador { get; set; }

        // Relación con Sala
        [Required(ErrorMessage = "Debe seleccionar una sala.")]
        public int SalaId { get; set; }
        public Sala? Sala { get; set; }

        [Required(ErrorMessage = "El campo {0} es obligatorio")]
        [Range(1, 500, ErrorMessage = "El {0} debe ser entre 1 y 500")]
        public int CupoMaximo { get; set; }


        // Relación con HorarioClase
        public virtual ICollection<HorarioClase> Horarios { get; set; } = new List<HorarioClase>();
    }
}

