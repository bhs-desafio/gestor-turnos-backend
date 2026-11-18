namespace GestorTurnos.Application.Features.Turnos.CargarTurnos;

public sealed record CargarTurnosResponse(
    Guid IdCarga,
    int TotalFilas,
    int FilasExitosas,
    int FilasFallidas,
    IReadOnlyList<CargarTurnosDetalleResponse> Detalles);

public sealed record CargarTurnosDetalleResponse(
    int Fila,
    string? Email,
    DateTime? HoraInicio,
    DateTime? HoraFin,
    bool Exitoso,
    string? Error,
    Guid? IdTurno);
