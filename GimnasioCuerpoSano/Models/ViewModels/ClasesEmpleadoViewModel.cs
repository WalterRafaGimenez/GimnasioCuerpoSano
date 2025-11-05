namespace GimnasioCuerpoSano.Models.ViewModels
{
    public class ClasesEmpleadoViewModel
    {
        public int MiembroId { get; set; }
        public string NombreCompletoMiembro { get; set; } = string.Empty;
        public List<InscripcionClaseViewModel> ClasesDisponibles { get; set; }
    }
}
