using FluentValidation;

namespace OpenBargain.Modules.Accounts.Application;

internal sealed record LoginRequest(string Email, string Password);

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).ValidEmail();
        RuleFor(r => r.Password).NotEmpty().WithMessage("Password is required.");
    }
}

// Every failure (unknown email, locked out, wrong password, suspended) is the same InvalidCredentials.
internal sealed class LoginUser(IUserAccounts users, IAccessTokenIssuer tokens)
{
    public async Task<Result<AuthResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim(), ct);
        if (user is null)
        {
            return AccountsError.InvalidCredentials;
        }

        if (await users.CheckPasswordAsync(user.Id, request.Password, ct) != PasswordCheck.Success)
        {
            return AccountsError.InvalidCredentials;
        }

        // Checked only after a correct password.
        if (user.IsSuspended)
        {
            return AccountsError.InvalidCredentials;
        }

        await users.ResetAccessFailedCountAsync(user.Id, ct);

        var token = tokens.Issue(user);
        return new AuthResponse(token.Token, token.ExpiresAt, UserResponse.From(user));
    }
}
