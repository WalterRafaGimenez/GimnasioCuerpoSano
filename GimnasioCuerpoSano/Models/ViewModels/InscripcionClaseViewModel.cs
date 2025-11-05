namespace GimnasioCuerpoSano.Models
{
    public class InscripcionClaseViewModel
    {
        public int Id { get; set; }

        public string? NombreMiembro { get; set; }
        public string? ApellidoMiembro { get; set; }
        public string? DNI { get; set; }

        // Información de la clase/hora
        public string? NombreClase { get; set; }
        public string? DiaSemana { get; set; }   // string, no enum
        public string? HoraInicio { get; set; }  // string para mostrar "hh:mm"
        public string? HoraFin { get; set; }     // string para mostrar "hh:mm"
        public string? NumeroSala { get; set; }
        public string? NombreEntrenador { get; set; }

        // Estado de inscripción
        public string? Estado { get; set; }      // "Inscripto" / "No Inscripto"

        public DateTime FechaInscripcion { get; set; }

        //Nueva propiedad para el botón Details
        public int? InscripcionId { get; set; }
        public bool EstaLleno { get; set; }  // true si ya se alcanzó el cupo máximo

    }
}
