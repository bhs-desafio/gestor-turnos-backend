using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;

namespace GestorTurnos.Domain.Repositories
{
    public interface ITurnoRepository : IRepository<Turno, Guid>
    {
        /// <summary>
        /// Obtiene los turnos de los colaboradores indicados que se cruzan con el rango [desde, hasta).
        /// </summary>
        Task<IReadOnlyList<Turno>> ObtenerPorColaboradoresEnRangoAsync(
            IReadOnlyCollection<Guid> idsColaborador,
            DateTime desde,
            DateTime hasta,
            CancellationToken cancellationToken = default);
    }
}
