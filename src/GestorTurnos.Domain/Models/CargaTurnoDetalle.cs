using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class CargaTurnoDetalle : Entity<Guid>
    {
        public Guid IdColaborador { get; private set; }
        public DateTime HoraInicio { get; private set; }
        public DateTime HoraFin { get; private set; }
        public string Error { get; private set; }
    }
}
