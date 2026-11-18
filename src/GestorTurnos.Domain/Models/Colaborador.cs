using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class Colaborador: Entity<Guid>
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Pais { get; set; }
    }
}
