using GestorTurnos.CrossCutting.Results;
using MediatR;

namespace GestorTurnos.Application.Common;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
