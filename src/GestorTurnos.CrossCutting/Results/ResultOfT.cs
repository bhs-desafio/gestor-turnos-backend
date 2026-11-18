namespace GestorTurnos.CrossCutting.Results;

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, Error error, T? value) : base(isSuccess, error) => Value = value;

    public static Result<T> Success(T value) => new(true, Error.None, value);

    public static new Result<T> Failure(Error error) => new(false, error, default);
}
