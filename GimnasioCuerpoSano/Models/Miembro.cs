using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace GimnasioCuerpoSano.Models
{
    public class Miembro : IValidatableObject
    {
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

        [Display(Name = "Tipo de membresía")]
        [ForeignKey("Membresia")]
        public int MembresiaId { get; set; }

        public Membresia? Membresia { get; set; }

        [Display(Name = "Es estudiante o jubilado")]
        public bool DescuentoEspecial { get; set; }

        [Display(Name = "Valor de membresía")]
        public decimal ValorMembresia { get; set; }

        [Display(Name = "Fecha y hora de alta")]
        public DateTime FechaAlta { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [Display(Name = "Foto")]
        public string? Foto { get; set; } = string.Empty;

        [Display(Name = "Código de barras")]
        public string? CodigoBarra { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [Display(Name = "Número de Documento")]
        [StringLength(12, ErrorMessage = "El DNI no puede superar los 12 caracteres")]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [Display(Name = "Tipo de Documento")]
        public string TipoDocumento { get; set; } = "DNI"; // valores: "DNI" o "DNI Extranjero"

        // RELACIÓN CON INSCRIPCIONES
        public virtual ICollection<InscripcionClase> Inscripciones { get; set; } = new List<InscripcionClase>();

        // ===============================
        // VALIDACIÓN PERSONALIZADA
        // ===============================
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoDocumento == "DNI")
            {
                if (!Regex.IsMatch(DNI, @"^\d{8}$"))
                    yield return new ValidationResult("El DNI debe tener exactamente 8 dígitos numéricos.", new[] { nameof(DNI) });
            }
            else if (TipoDocumento == "DNI Extranjero")
            {
                if (!Regex.IsMatch(DNI, @"^\d{8}[A-Za-z0-9]{1}$"))
                    yield return new ValidationResult("El DNI extranjero debe tener 8 números seguidos de un carácter (número o letra, sin símbolos).", new[] { nameof(DNI) });
            }
        }
    }
}



