using FluentValidation;

namespace GestorTurnos.Application.Features.Turnos.CargarTurnos;

public sealed class CargarTurnosValidator : AbstractValidator<CargarTurnosCommand>
{
    public CargarTurnosValidator()
    {
        RuleFor(x => x.NombreArchivo)
            .NotEmpty().WithMessage("El nombre del archivo es requerido")
            .Must(nombre => nombre is not null && nombre.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("El archivo debe tener extensión .xlsx");
        RuleFor(x => x.Archivo)
            .Must(archivo => archivo is { Length: > 0 }).WithMessage("El archivo está vacío");
    }
}
