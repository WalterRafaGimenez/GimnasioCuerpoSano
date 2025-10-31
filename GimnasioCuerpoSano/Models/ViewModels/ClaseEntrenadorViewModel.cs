namespace GimnasioCuerpoSano.Models.ViewModels
{
    public class ClaseEntrenadorViewModel
    {
        public string? NombreClase { get; set; }
        public string? SalaNombre { get; set; }
        public string? DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
        public DateTime? FechaVencimientoCertificado { get; set; }
    }
}

