using GestorTurnos.CrossCutting.Results;
using MediatR;

namespace GestorTurnos.Application.Common;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
