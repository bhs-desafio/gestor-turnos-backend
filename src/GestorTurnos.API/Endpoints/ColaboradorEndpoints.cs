using GestorTurnos.API.Common;
using GestorTurnos.Application.Features.Colaboradores.CrearColabarador;
using GestorTurnos.CrossCutting.Results;
using MediatR;

namespace GestorTurnos.API.Endpoints;

public static class ColaboradorEndpoints
{
    public static WebApplication MapColaboradorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/colaboradores")
            .WithTags("Colaboradores");

        group.MapPost("/", async (
            CrearColaboradorCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("CrearColaborador")
        .WithSummary("Crear colaborador")
        .WithDescription(
            "Registra un nuevo colaborador. Nombre, Email y País son obligatorios y el email debe tener un formato válido. " +
            "Devuelve el Id del colaborador creado.")
        .Accepts<CrearColaboradorCommand>("application/json")
        .Produces<CrearColaboradorResponse>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status400BadRequest)
        .Produces<Error>(StatusCodes.Status500InternalServerError);

        return app;
    }
}
