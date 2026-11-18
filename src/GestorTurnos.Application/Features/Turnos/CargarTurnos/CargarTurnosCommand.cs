using GestorTurnos.Application.Common;

namespace GestorTurnos.Application.Features.Turnos.CargarTurnos;

public sealed record CargarTurnosCommand(string NombreArchivo, byte[] Archivo)
            : ICommand<CargarTurnosResponse>;
