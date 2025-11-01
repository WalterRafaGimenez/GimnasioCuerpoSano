namespace GimnasioCuerpoSano.Models.ViewModels
{
    public class ClasesEmpleadoViewModel
    {
        public int MiembroId { get; set; }
        public string NombreCompletoMiembro { get; set; } = string.Empty;
        public List<HorarioClase> ClasesDisponibles { get; set; } = new List<HorarioClase>();
    }
}
