namespace OpenBargain.Modules.Accounts.Application;

// Revokes the presented refresh token, if it is valid. Never fails.
internal sealed class Logout(IRefreshTokenStore store, TimeProvider time)
{
    public async Task ExecuteAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var stored = await store.FindByHashAsync(RefreshTokens.Hash(refreshToken), ct);
        if (stored is { RevokedAt: null })
        {
            await store.TryRevokeAsync(stored.Id, time.GetUtcNow(), replacedByTokenId: null, ct);
        }
    }
}
