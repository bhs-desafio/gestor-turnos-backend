using GestorTurnos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Infrastructure.Persistence.Repositories;

public class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected readonly GestorTurnosDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(GestorTurnosDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => DbSet.Update(entity);

    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
