using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Infrastructure.Persistence.Repositories
{
    public class ColaboradorRepository: Repository<Colaborador, Guid>, IColaboradorRepository
    {

        public ColaboradorRepository(GestorTurnosDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Colaborador>> ObtenerPorEmailsAsync(
            IReadOnlyCollection<string> emails,
            CancellationToken cancellationToken = default) =>
            await DbSet
                .AsNoTracking()
                .Where(c => emails.Contains(c.Email))
                .ToListAsync(cancellationToken);
    }
}
