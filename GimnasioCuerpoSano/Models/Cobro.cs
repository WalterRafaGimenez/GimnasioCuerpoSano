using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GimnasioCuerpoSano.Models
{
    public class Cobro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Código de Cobro")]
        public string Codigo { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required]
        [Display(Name = "Fecha de Pago")]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Método de Pago")]
        public string MetodoPago { get; set; } = string.Empty;
        // Efectivo, Transferencia, MercadoPago, Tarjeta Crédito, Tarjeta Débito

        [Required]
        [Display(Name = "Estado del Pago")]
        public string Estado { get; set; } = "Vigente";
        // “Vigente” o “Vencido” (según membresía y fecha)

        // Relaciones
        [Required]
        [Display(Name = "Miembro")]
        public int MiembroId { get; set; }

        [ForeignKey("MiembroId")]
        public Miembro? Miembro { get; set; }

        [Required]
        [Display(Name = "Membresía")]
        public int MembresiaId { get; set; }

        [ForeignKey("MembresiaId")]
        public Membresia? Membresia { get; set; }
    }
}

