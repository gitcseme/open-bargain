namespace OpenBargain.Modules.Accounts.Infrastructure;

// A row in accounts."RefreshTokens". The raw token is never stored, only its SHA-256 hash.
internal sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    // The token that replaced this one on rotation.
    public Guid? ReplacedByTokenId { get; set; }
}
