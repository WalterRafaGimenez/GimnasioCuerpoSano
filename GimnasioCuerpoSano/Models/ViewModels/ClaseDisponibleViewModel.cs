namespace GimnasioCuerpoSano.Models.ViewModels
{
    public class ClaseDisponibleViewModel
    {
        public int HorarioClaseId { get; set; }
        public string? NombreClase { get; set; }
        public string? SalaNombre { get; set; }
        public string? DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public string? EntrenadorNombre { get; set; }
        public bool EstaInscripto { get; set; } // Para mostrar Inscribirse / Desinscribirse
        public bool EstaLleno { get; set; }

    }

}