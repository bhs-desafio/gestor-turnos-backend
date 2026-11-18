using GestorTurnos.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Infrastructure.Persistence;

public class GestorTurnosDbContext : DbContext
{
    public GestorTurnosDbContext(DbContextOptions<GestorTurnosDbContext> options) : base(options)
    {
        
    }

    public DbSet<Colaborador> Colaborador => Set<Colaborador>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GestorTurnosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
