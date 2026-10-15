namespace LiquidStock.Api.Services;

public class ServiceResult<T>
{
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public int StatusCode { get; private init; } = 200;
    public bool Succeeded => Error is null;

    public static ServiceResult<T> Ok(T value) => new() { Value = value };
    public static ServiceResult<T> Fail(int statusCode, string error) =>
        new() { StatusCode = statusCode, Error = error };
}
