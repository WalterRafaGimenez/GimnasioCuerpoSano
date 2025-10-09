using System;
using System.ComponentModel.DataAnnotations;

namespace GimnasioCuerpoSano.Models
{
    public class Miembro
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(10, ErrorMessage = "El nombre no puede tener más de 10 caracteres")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [MaxLength(15, ErrorMessage = "El apellido no puede tener más de 15 caracteres")]
        public required string Apellido { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [MaxLength(50, ErrorMessage = "La dirección no puede tener más de 50 caracteres")]
        public required string Direccion { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 números")]
        public required string Telefono { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido con @")]
        public required string Mail { get; set; }
    }
}

