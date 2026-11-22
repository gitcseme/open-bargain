namespace OpenBargain.Modules.Accounts.Application;

internal enum PasswordCheck
{
    Success,
    WrongPassword,
    LockedOut,
}

// User storage and password checks, implemented in Infrastructure with Identity.
internal interface IUserAccounts
{
    Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken ct);

    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct);

    // True when another user (not exceptUserId) has this normalized display name.
    Task<bool> IsDisplayNameTakenAsync(string normalizedDisplayName, Guid? exceptUserId, CancellationToken ct);

    // Fails with EmailTaken or DisplayNameTaken, including when a parallel request wins the race.
    Task<Result<UserAccount>> CreateAsync(string email, string password, string displayName, CancellationToken ct);

    // A wrong password counts towards lockout.
    Task<PasswordCheck> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);

    Task ResetAccessFailedCountAsync(Guid userId, CancellationToken ct);
}
