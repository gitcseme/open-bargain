using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.UnitTests.Accounts;

public class RefreshTokensTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_returns_a_random_base64url_token_with_its_hash_and_expiry()
    {
        var token = RefreshTokens.Create(Now, lifetimeDays: 7);

        Assert.Equal(43, token.Token.Length); // 32 bytes, base64url without padding
        Assert.Matches("^[A-Za-z0-9_-]+$", token.Token);
        Assert.Equal(RefreshTokens.Hash(token.Token), token.TokenHash);
        Assert.Equal(Now, token.CreatedAt);
        Assert.Equal(Now.AddDays(7), token.ExpiresAt);
        Assert.NotEqual(token.Token, RefreshTokens.Create(Now, 7).Token);
    }

    [Fact]
    public void Hash_is_lowercase_hex_sha256() =>
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            RefreshTokens.Hash("abc"));
}
