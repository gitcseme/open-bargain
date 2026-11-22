using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.Modules.Accounts.Infrastructure;

internal sealed class RefreshTokenStore(AccountsDbContext db) : IRefreshTokenStore
{
    public async Task<IUnitOfWork> BeginTransactionAsync(CancellationToken ct) =>
        new EfUnitOfWork(await db.Database.BeginTransactionAsync(ct));

    public Task<StoredRefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => new StoredRefreshToken(t.Id, t.UserId, t.ExpiresAt, t.RevokedAt))
            .SingleOrDefaultAsync(ct);

    public async Task AddAsync(Guid userId, NewRefreshToken token, CancellationToken ct)
    {
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = token.Id,
            UserId = userId,
            TokenHash = token.TokenHash,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
        });
        await db.SaveChangesAsync(ct);
    }

    // The "RevokedAt IS NULL" condition makes this safe under parallel requests: in PostgreSQL the
    // second UPDATE waits for the first to commit, then matches no row.
    public async Task<bool> TryRevokeAsync(Guid id, DateTimeOffset now, Guid? replacedByTokenId, CancellationToken ct) =>
        await db.RefreshTokens
            .Where(t => t.Id == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.ReplacedByTokenId, replacedByTokenId), ct) == 1;

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    private sealed class EfUnitOfWork(IDbContextTransaction transaction) : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
