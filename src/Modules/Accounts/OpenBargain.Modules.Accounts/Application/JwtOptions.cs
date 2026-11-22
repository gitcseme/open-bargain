namespace OpenBargain.Modules.Accounts.Application;

// Bound from the "Jwt" config section. The host validates tokens with the same values.
internal sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "open-bargain";
    public string Audience { get; set; } = "open-bargain";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
