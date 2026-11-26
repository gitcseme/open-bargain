namespace OpenBargain.Modules.Accounts.Application;

internal sealed record UserResponse(Guid Id, string Email, string DisplayName, string? AvatarUrl, string Role)
{
    public static UserResponse From(UserAccount user) =>
        new(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.Role.ToString());
}

internal sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
