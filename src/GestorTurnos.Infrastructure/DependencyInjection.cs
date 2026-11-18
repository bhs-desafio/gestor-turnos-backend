using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Repositories;
using GestorTurnos.Infrastructure.Persistence;
using GestorTurnos.Infrastructure.Persistence.Initialization;
using GestorTurnos.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorTurnos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GestorTurnosDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("GestorTurnosDb")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IColaboradorRepository, ColaboradorRepository>();

        services.AddSingleton(LeerOpcionesDeBaseDeDatos(configuration));
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// Crea la base de datos si no existe (o aplica migraciones pendientes). Llamar al iniciar la aplicación.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }

    private static DatabaseInitializerOptions LeerOpcionesDeBaseDeDatos(IConfiguration configuration)
    {
        var section = configuration.GetSection(DatabaseInitializerOptions.SectionName);
        var options = new DatabaseInitializerOptions();

        if (bool.TryParse(section[nameof(DatabaseInitializerOptions.CreateIfNotExists)], out var crear))
            options.CreateIfNotExists = crear;
        if (int.TryParse(section[nameof(DatabaseInitializerOptions.MaxRetries)], out var reintentos))
            options.MaxRetries = reintentos;
        if (int.TryParse(section[nameof(DatabaseInitializerOptions.RetryDelaySeconds)], out var espera))
            options.RetryDelaySeconds = espera;

        return options;
    }
}
