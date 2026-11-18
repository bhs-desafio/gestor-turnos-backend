using GestorTurnos.API.Common;
using GestorTurnos.Application.Features.Turnos.CargarTurnos;
using GestorTurnos.CrossCutting.Results;
using MediatR;

namespace GestorTurnos.API.Endpoints;

public static class TurnoEndpoints
{
    public static WebApplication MapTurnoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/turnos")
            .WithTags("Turnos");

        group.MapPost("/carga", async (
            IFormFile archivo,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria, cancellationToken);

            var command = new CargarTurnosCommand(archivo.FileName, memoria.ToArray());
            var result = await mediator.Send(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("CargarTurnos")
        .WithSummary("Cargar turnos desde Excel")
        .WithDescription(
            "Recibe un archivo .xlsx (campo de formulario 'archivo') con las columnas email, fechaInicio y fechaFin. " +
            "Cada fila se procesa de forma independiente: si es válida se registra el turno; si falla se guarda el motivo. " +
            "Los turnos de un mismo colaborador no pueden solaparse. Devuelve el resultado de cada fila.")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<CargarTurnosResponse>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status400BadRequest)
        .Produces<Error>(StatusCodes.Status500InternalServerError)
        .DisableAntiforgery();

        return app;
    }
}
