namespace OpenBargain.Modules.Accounts.Application;

internal sealed class GetMe(IUserAccounts users)
{
    public async Task<Result<UserResponse>> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null || user.IsSuspended)
        {
            return AccountsError.UserUnavailable;
        }

        return UserResponse.From(user);
    }
}
