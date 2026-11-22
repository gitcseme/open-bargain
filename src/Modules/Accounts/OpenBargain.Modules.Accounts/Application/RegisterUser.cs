using FluentValidation;

namespace OpenBargain.Modules.Accounts.Application;

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email).ValidEmail();
        RuleFor(r => r.Password)
            .NotNull().WithMessage("Password is required.")
            .Length(8, 128).WithMessage("Password must be 8–128 characters.");
        RuleFor(r => r.DisplayName).ValidDisplayName();
    }
}

// Creates the user only: no token and no cookie (the client logs in next).
internal sealed class RegisterUser(IUserAccounts users)
{
    public async Task<Result<UserResponse>> ExecuteAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();

        if (await users.FindByEmailAsync(email, ct) is not null)
        {
            return AccountsError.EmailTaken;
        }

        if (await users.IsDisplayNameTakenAsync(DisplayNames.Normalize(displayName), exceptUserId: null, ct))
        {
            return AccountsError.DisplayNameTaken;
        }

        var created = await users.CreateAsync(email, request.Password, displayName, ct);
        return created.IsSuccess ? UserResponse.From(created.Value) : created.Error!.Value;
    }
}
