using GestorTurnos.CrossCutting.Results;

namespace GestorTurnos.API.Common;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.Ok() : MapError(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error);

    private static IResult MapError(Error error) => error.Type switch
    {
        ErrorType.Validation => Results.BadRequest(error),
        ErrorType.NotFound => Results.NotFound(error),
        ErrorType.Conflict => Results.Conflict(error),
        ErrorType.Unauthorized => Results.Json(error, statusCode: StatusCodes.Status401Unauthorized),
        _ => Results.Json(error, statusCode: StatusCodes.Status500InternalServerError)
    };
}
