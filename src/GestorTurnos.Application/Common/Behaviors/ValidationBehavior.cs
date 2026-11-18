using System.Reflection;
using GestorTurnos.CrossCutting.Results;
using FluentValidation;
using MediatR;

namespace GestorTurnos.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly MethodInfo FailureMethod =
        typeof(TResponse).GetMethod(
            nameof(Result.Failure),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(Error)],
            modifiers: null)
        ?? throw new InvalidOperationException(
            $"{typeof(TResponse).Name} does not expose a static Failure(Error) method.");

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var message = string.Join(" ", failures.Select(failure => failure.ErrorMessage));
        var error = new Error("Validation.Failed", message, ErrorType.Validation);

        return (TResponse)FailureMethod.Invoke(null, [error])!;
    }
}
