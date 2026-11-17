using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class Colaborador: Entity<Guid>
    {
        public string Nombre { get; private set; }
        public string Email { get; private set; }
        public string Pais { get; private set; }
    }
}
