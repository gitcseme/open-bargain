using FluentValidation;
using Microsoft.Extensions.Options;

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
internal sealed class LoginUser(
    IUserAccounts users,
    IAccessTokenIssuer accessTokens,
    IRefreshTokenStore refreshTokens,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    public async Task<Result<Session>> ExecuteAsync(LoginRequest request, CancellationToken ct)
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

        var refresh = RefreshTokens.Create(time.GetUtcNow(), options.Value.RefreshTokenDays);
        await refreshTokens.AddAsync(user.Id, refresh, ct);

        var access = accessTokens.Issue(user);
        return new Session(new AuthResponse(access.Token, access.ExpiresAt, UserResponse.From(user)), refresh.Token, refresh.ExpiresAt);
    }
}
