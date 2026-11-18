using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Infrastructure.Persistence.Repositories;

public class TurnoRepository : Repository<Turno, Guid>, ITurnoRepository
{
    public TurnoRepository(GestorTurnosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Turno>> ObtenerPorColaboradoresEnRangoAsync(
        IReadOnlyCollection<Guid> idsColaborador,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Where(t => idsColaborador.Contains(t.IdColaborador)
                        && t.HoraInicio < hasta
                        && t.HoraFin > desde)
            .ToListAsync(cancellationToken);
}
