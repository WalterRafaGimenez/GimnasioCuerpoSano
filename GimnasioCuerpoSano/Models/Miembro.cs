using System;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Miembro
    {
        // -----------------------------
        // Campos obligatorios inicializados
        // -----------------------------
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(10)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [MaxLength(15)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [MaxLength(50)]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [MaxLength(10)]
        [RegularExpression(@"^\d+$", ErrorMessage = "Solo se permiten números")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mail es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe ser un correo válido")]
        public string Mail { get; set; } = string.Empty;

        // -----------------------------
        // Campos de membresía
        // -----------------------------
        [Required(ErrorMessage = "Seleccione un tipo de membresía")]
        public string TipoMembresia { get; set; } = string.Empty; // Mensual, Trimestral, Anual

        [Display(Name = "Es estudiante o jubilado")]
        public bool DescuentoEspecial { get; set; } // true = 15% de descuento

        [Display(Name = "Valor de membresía")]
        public decimal ValorMembresia { get; set; } // Valor calculado según tipo y descuento

        [Display(Name = "Fecha y hora de alta")]
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        [Display(Name = "Foto (opcional)")]
        public string? Foto { get; set; } = string.Empty; // Opcional, ruta o nombre de archivo, no obligatorio
    }
}


