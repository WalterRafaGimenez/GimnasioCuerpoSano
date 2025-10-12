using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Membresia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la membresía es obligatorio")]
        [MaxLength(30)]
        public string Nombre { get; set; } = string.Empty; // Mensual, Trimestral, etc.

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }

        // Relación: una membresía puede tener muchos miembros
        public ICollection<Miembro>? Miembros { get; set; }
    }
}
