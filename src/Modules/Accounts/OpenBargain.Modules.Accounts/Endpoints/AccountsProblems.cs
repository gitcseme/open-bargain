using Microsoft.AspNetCore.Http;
using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.Modules.Accounts.Endpoints;

// Maps use-case errors to RFC 9457 ProblemDetails with a stable "code".
internal static class AccountsProblems
{
    public static IResult For(AccountsError error) => error switch
    {
        AccountsError.EmailTaken =>
            Problem(StatusCodes.Status409Conflict, "An account with this email already exists.", "email_taken"),
        AccountsError.DisplayNameTaken =>
            Problem(StatusCodes.Status409Conflict, "This display name is taken.", "display_name_taken"),
        AccountsError.InvalidCredentials =>
            Problem(StatusCodes.Status401Unauthorized, "Email or password is incorrect.", "invalid_credentials"),
        AccountsError.InvalidRefreshToken =>
            Problem(StatusCodes.Status401Unauthorized, "The session has expired. Log in again.", "invalid_refresh_token"),
        AccountsError.UserUnavailable => Results.Unauthorized(),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
    };

    private static IResult Problem(int status, string title, string code) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
}
