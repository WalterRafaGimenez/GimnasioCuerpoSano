namespace GimnasioCuerpoSano.Models.ViewModels
{
 
    public class MisClasesEntrenadorViewModel
    {
        public DateTime? FechaVencimientoCertificado { get; set; }
        public IEnumerable<ClaseEntrenadorViewModel> Clases { get; set; } = new List<ClaseEntrenadorViewModel>();
    }
}
