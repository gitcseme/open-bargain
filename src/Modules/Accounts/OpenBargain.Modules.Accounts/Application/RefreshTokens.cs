using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace OpenBargain.Modules.Accounts.Application;

// A new refresh token. Only TokenHash is stored; Token goes to the client in the ob_refresh cookie.
internal sealed record NewRefreshToken(Guid Id, string Token, string TokenHash, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

internal static class RefreshTokens
{
    public static NewRefreshToken Create(DateTimeOffset now, int lifetimeDays)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return new NewRefreshToken(Guid.CreateVersion7(now), token, Hash(token), now, now.AddDays(lifetimeDays));
    }

    // Lowercase hex SHA-256 of the raw token.
    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
