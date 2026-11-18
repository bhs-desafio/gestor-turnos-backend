
using GestorTurnos.Domain.Common;

namespace GestorTurnos.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly GestorTurnosDbContext _dbContext;

    public UnitOfWork(GestorTurnosDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
