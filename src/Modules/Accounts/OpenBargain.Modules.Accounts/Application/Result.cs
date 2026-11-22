namespace OpenBargain.Modules.Accounts.Application;

// Expected failures a use case can return. Endpoints map them to ProblemDetails.
internal enum AccountsError
{
    EmailTaken,
    DisplayNameTaken,
    InvalidCredentials,
    InvalidRefreshToken,

    // The token's user no longer exists or is suspended.
    UserUnavailable,
}

// Either a value or an error. Use cases return this instead of throwing for expected failures.
internal readonly struct Result<T>
{
    private readonly T? _value;

    private Result(T? value, AccountsError? error)
    {
        _value = value;
        Error = error;
    }

    public AccountsError? Error { get; }

    public bool IsSuccess => Error is null;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"The result failed with {Error}.");

    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(AccountsError error) => new(default, error);
}
