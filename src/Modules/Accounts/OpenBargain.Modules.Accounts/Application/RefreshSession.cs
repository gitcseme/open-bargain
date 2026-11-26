using Microsoft.Extensions.Options;

namespace OpenBargain.Modules.Accounts.Application;

// Rotates the refresh token: the old one is revoked and a new one issued, with a fresh
// 7-day expiry (sliding). Presenting a revoked token is treated as theft: all of the
// user's tokens are revoked.
internal sealed class RefreshSession(
    IRefreshTokenStore store,
    IUserAccounts users,
    IAccessTokenIssuer accessTokens,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    public async Task<Result<Session>> ExecuteAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return AccountsError.InvalidRefreshToken;
        }

        var now = time.GetUtcNow();
        await using var transaction = await store.BeginTransactionAsync(ct);

        var stored = await store.FindByHashAsync(RefreshTokens.Hash(refreshToken), ct);
        if (stored is null)
        {
            return AccountsError.InvalidRefreshToken;
        }

        if (stored.RevokedAt is not null)
        {
            return await RevokeAllAsync(stored.UserId);
        }

        if (stored.ExpiresAt <= now)
        {
            return AccountsError.InvalidRefreshToken;
        }

        var user = await users.FindByIdAsync(stored.UserId, ct);
        if (user is null || user.IsSuspended)
        {
            return await RevokeAllAsync(stored.UserId);
        }

        var next = RefreshTokens.Create(now, options.Value.RefreshTokenDays);
        if (!await store.TryRevokeAsync(stored.Id, now, next.Id, ct))
        {
            // A parallel request rotated this token first: same as reuse.
            return await RevokeAllAsync(stored.UserId);
        }

        await store.AddAsync(user.Id, next, ct);
        await transaction.CommitAsync(ct);

        var access = accessTokens.Issue(user);
        return new Session(new AuthResponse(access.Token, access.ExpiresAt, UserResponse.From(user)), next.Token, next.ExpiresAt);

        async Task<Result<Session>> RevokeAllAsync(Guid userId)
        {
            await store.RevokeAllForUserAsync(userId, now, ct);
            await transaction.CommitAsync(ct);
            return AccountsError.InvalidRefreshToken;
        }
    }
}
