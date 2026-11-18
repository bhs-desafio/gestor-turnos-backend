using GestorTurnos.CrossCutting.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace GestorTurnos.API.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        var error = new Error("Unexpected", "An unexpected error occurred.", ErrorType.Unexpected);
        var result = Result.Failure(error).ToHttpResult();

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
