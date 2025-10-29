namespace GimnasioCuerpoSano.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Asistencia
    {
        [Key]
        public int ID_Asistencia { get; set; }

        [Column("ID_Miembro")]
        [ForeignKey("Miembro")]
        public int ID_Miembro { get; set; }

        public Miembro Miembro { get; set; }

        public DateTime FechaHora { get; set; }

        public string EstadoMembresia { get; set; }
    }
}


