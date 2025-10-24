using System;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Entrenador
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string Apellido { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8,10}$", ErrorMessage = "El teléfono debe tener entre 8 y 10 dígitos")]

        public string Telefono { get; set; } = null!;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        public string Email { get; set; } = null!;

        public string Especialidad { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? FechaVencimientoCertificado { get; set; }

        // Nueva propiedad para guardar la ruta del archivo subido
        public string? RutaCertificado { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [Display(Name = "Número de Documento")]
        [StringLength(12, ErrorMessage = "El DNI no puede superar los 12 caracteres")]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [Display(Name = "Tipo de Documento")]
        public string TipoDocumento { get; set; } = "DNI"; // valores: "DNI" o "DNI-Extranjero"

    }
}

