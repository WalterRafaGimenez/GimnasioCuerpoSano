using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace GimnasioCuerpoSano.Models
{
        public enum DiaSemana
    {
        [Display(Name = "Lunes")]
        Lunes = 1,

        [Display(Name = "Martes")]
        Martes,

        [Display(Name = "Miércoles")]
        Miercoles,

        [Display(Name = "Jueves")]
        Jueves,

        [Display(Name = "Viernes")]
        Viernes,

        [Display(Name = "Sábado")]
        Sabado

        // Puedes agregar Domingo si es necesario
    }
    public class HorarioClase
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La clase es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una clase.")]
        public int ClaseId { get; set; }
        // Se usa 'null!' (Null-Forgiving Operator) porque la FK [Required] asegura que existirá.
        [ValidateNever]
        public virtual Clase Clase { get; set; } = null!;

        [Required(ErrorMessage = "La sala es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una sala.")]
        public int SalaId { get; set; }
        // Se usa 'null!' por la misma razón que Clase.
        [ValidateNever]
        public virtual Sala Sala { get; set; } = null!;

        [Required(ErrorMessage = "El día de la semana es obligatorio.")]
        [EnumDataType(typeof(DiaSemana))]
        [Range(1, 6, ErrorMessage = "Debe seleccionar un día válido.")]
        public DiaSemana DiaSemana { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        //PROPIEDAD AGREGADA (Sincronizada con la DB)
        [Required(ErrorMessage = "La capacidad máxima es obligatoria.")]
        [Range(1, 1000, ErrorMessage = "La capacidad debe ser un número entre 1 y 1000.")]
        public int CapacidadMaxima { get; set; }

        //[Required]
        public bool SePuedeInscribir { get; set; } = true;

        // Relación con InscripcionClase
        public virtual ICollection<InscripcionClase> Inscripciones { get; set; } = new List<InscripcionClase>();
    }
}