using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{
    public class Turno: Entity<Guid>
    {
        public Guid IdColaborador { get; private set; }
        public DateTime HoraInicio { get; private set; }
        public DateTime HoraFin { get; private set; }   
    }
}
