namespace OpenBargain.Modules.Accounts.Application;

// What login and refresh return: the response body plus the refresh token for the cookie.
internal sealed record Session(AuthResponse Auth, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
