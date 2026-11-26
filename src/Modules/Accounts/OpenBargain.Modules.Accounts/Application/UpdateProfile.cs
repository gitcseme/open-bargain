using FluentValidation;

namespace OpenBargain.Modules.Accounts.Application;

internal sealed record UpdateProfileRequest(string DisplayName);

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator() => RuleFor(r => r.DisplayName).ValidDisplayName();
}

// Only the display name can change. Keeping your own name, or changing only its case, is allowed.
internal sealed class UpdateProfile(IUserAccounts users)
{
    public async Task<Result<UserResponse>> ExecuteAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null || user.IsSuspended)
        {
            return AccountsError.UserUnavailable;
        }

        var displayName = request.DisplayName.Trim();
        if (await users.IsDisplayNameTakenAsync(DisplayNames.Normalize(displayName), exceptUserId: userId, ct))
        {
            return AccountsError.DisplayNameTaken;
        }

        var updated = await users.UpdateDisplayNameAsync(userId, displayName, ct);
        return updated.IsSuccess ? UserResponse.From(updated.Value) : updated.Error!.Value;
    }
}
