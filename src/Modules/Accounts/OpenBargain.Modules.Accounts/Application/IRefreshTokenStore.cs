namespace OpenBargain.Modules.Accounts.Application;

internal sealed record StoredRefreshToken(Guid Id, Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

internal interface IUnitOfWork : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}

// Refresh token storage, implemented in Infrastructure with EF Core.
internal interface IRefreshTokenStore
{
    // Everything done through this store and IUserAccounts until commit runs in one transaction.
    Task<IUnitOfWork> BeginTransactionAsync(CancellationToken ct);

    Task<StoredRefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct);

    Task AddAsync(Guid userId, NewRefreshToken token, CancellationToken ct);

    // Revokes the token if it is still unrevoked. False when it was already revoked,
    // for example by a parallel request that won the race.
    Task<bool> TryRevokeAsync(Guid id, DateTimeOffset now, Guid? replacedByTokenId, CancellationToken ct);

    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct);
}
