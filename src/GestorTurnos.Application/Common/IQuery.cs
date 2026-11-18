using GestorTurnos.CrossCutting.Results;
using MediatR;

namespace GestorTurnos.Application.Common;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
