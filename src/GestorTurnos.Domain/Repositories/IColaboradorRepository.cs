using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;

namespace GestorTurnos.Domain.Repositories
{
    public interface IColaboradorRepository: IRepository<Colaborador, Guid>
    {
        Task<IReadOnlyList<Colaborador>> ObtenerPorEmailsAsync(
            IReadOnlyCollection<string> emails,
            CancellationToken cancellationToken = default);
    }
}
