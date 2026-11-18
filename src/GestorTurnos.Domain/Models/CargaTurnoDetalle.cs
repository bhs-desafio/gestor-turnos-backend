using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class CargaTurnoDetalle : Entity<Guid>
    {
        public Guid IdCargaTurno { get; set; }
        public int Fila { get; set; }

        public string? Email { get; set; }
        public Guid? IdColaborador { get; set; }
        public DateTime? HoraInicio { get; set; }
        public DateTime? HoraFin { get; set; }
        public bool Exitoso { get; set; }

        public string? Error { get; set; }
        public Guid? IdTurno { get; set; }
    }
}
