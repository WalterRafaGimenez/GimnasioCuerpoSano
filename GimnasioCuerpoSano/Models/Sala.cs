using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GimnasioCuerpoSano.Models
{
    public class Sala
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_Sala { get; set; } // Se genera automáticamente

        [Required(ErrorMessage = "El número de sala es obligatorio.")]
        public string Numero { get; set; } = string.Empty; // ej. Sala 1, Sala 2

        [Required(ErrorMessage = "El número o nombre de la sala es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de la sala no puede superar los 50 caracteres.")]
        public string? Nombre { get; set; } // opcional

        [Required(ErrorMessage = "La capacidad máxima es obligatoria.")]
        [Range(1, 200, ErrorMessage = "La capacidad debe ser mayor a 0.")]
        public int? CapacidadMaxima { get; set; }

        [Required(ErrorMessage = "La ubicación es obligatoria.")]
        public string Ubicacion { get; set; } = string.Empty;

        public string? TipoSala { get; set; } // opcional

        [Required]
        public bool Disponible { get; set; } = true; // por defecto sí
    }
}

