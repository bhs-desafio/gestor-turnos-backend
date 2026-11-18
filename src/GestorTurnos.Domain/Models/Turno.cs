using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{
    public class Turno: Entity<Guid>
    {
        public Guid IdColaborador { get;  set; }
        public DateTime HoraInicio { get;  set; }
        public DateTime HoraFin { get;  set; }   
    }
}
