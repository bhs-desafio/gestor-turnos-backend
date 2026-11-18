using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorTurnos.Infrastructure.Persistence.Initialization;

/// <summary>
/// Estrategia de creación de la base de datos al iniciar la aplicación:
/// <list type="bullet">
/// <item>Si el proyecto tiene migraciones de EF Core: aplica las pendientes (crea la base de datos si no existe).</item>
/// <item>Si no hay migraciones: crea la base de datos y el esquema a partir del modelo, solo si no existe.</item>
/// </list>
/// Reintenta cuando el servidor todavía no acepta conexiones (por ejemplo, un contenedor de SQL Server iniciando).
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly GestorTurnosDbContext _dbContext;
    private readonly DatabaseInitializerOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        GestorTurnosDbContext dbContext,
        DatabaseInitializerOptions options,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CreateIfNotExists)
        {
            _logger.LogInformation("Inicialización de base de datos deshabilitada ({Section}:{Option} = false).",
                DatabaseInitializerOptions.SectionName, nameof(DatabaseInitializerOptions.CreateIfNotExists));
            return;
        }

        for (var intento = 1; ; intento++)
        {
            try
            {
                await CrearOMigrarAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && intento <= _options.MaxRetries)
            {
                _logger.LogWarning(ex,
                    "No se pudo inicializar la base de datos (intento {Intento} de {Total}). Reintentando en {Segundos} s...",
                    intento, _options.MaxRetries + 1, _options.RetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.RetryDelaySeconds), cancellationToken);
            }
        }
    }

    private async Task CrearOMigrarAsync(CancellationToken cancellationToken)
    {
        var database = _dbContext.Database;

        if (database.GetMigrations().Any())
        {
            var pendientes = (await database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pendientes.Count == 0)
            {
                _logger.LogInformation("La base de datos está actualizada; no hay migraciones pendientes.");
                return;
            }

            _logger.LogInformation("Aplicando {Cantidad} migración(es): {Migraciones}",
                pendientes.Count, string.Join(", ", pendientes));
            await database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migraciones aplicadas correctamente.");
            return;
        }

        var creada = await database.EnsureCreatedAsync(cancellationToken);
        if (creada)
            _logger.LogInformation("La base de datos no existía y fue creada a partir del modelo.");
        else
            _logger.LogInformation("La base de datos ya existe; no se realizaron cambios.");
    }
}
