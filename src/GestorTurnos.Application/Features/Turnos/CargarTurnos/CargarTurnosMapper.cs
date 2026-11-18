using AutoMapper;
using GestorTurnos.Domain.Models;

namespace GestorTurnos.Application.Features.Turnos.CargarTurnos;

public sealed class CargarTurnosMapper : Profile
{
    public CargarTurnosMapper()
    {
        CreateMap<CargaTurnoDetalle, CargarTurnosDetalleResponse>();
    }
}
