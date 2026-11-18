using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;

namespace GestorTurnos.Infrastructure.Persistence.Repositories;

public class CargaTurnoRepository : Repository<CargaTurno, Guid>, ICargaTurnoRepository
{
    public CargaTurnoRepository(GestorTurnosDbContext dbContext) : base(dbContext)
    {
    }
}
