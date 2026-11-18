namespace GestorTurnos.Infrastructure.Persistence.Initialization;

/// <summary>
/// Configuración de la inicialización de la base de datos (sección "Database" del appsettings).
/// </summary>
public sealed class DatabaseInitializerOptions
{
    public const string SectionName = "Database";

    /// <summary>Si es true, al iniciar la aplicación se crea la base de datos cuando no existe.</summary>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>Cantidad de reintentos si el servidor de base de datos aún no está disponible.</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>Segundos de espera entre reintentos.</summary>
    public int RetryDelaySeconds { get; set; } = 5;
}
