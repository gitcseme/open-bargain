using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace OpenBargain.Api.Setup;

// Accounts issues access tokens; the host validates them with the same "Jwt" settings.
// Accounts checks the signing key at startup (JwtOptions validation).
internal static class AuthenticationSetup
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    // Options are read when first used, so tests can override the settings.
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration, TimeProvider>((options, config, time) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"] ?? "")),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ClockSkew = ClockSkew,
                    // Checks expiry against the app's TimeProvider rather than the system clock.
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = time.GetUtcNow().UtcDateTime;
                        return (notBefore is null || notBefore.Value <= now + ClockSkew)
                            && expires is not null && expires.Value > now - ClockSkew;
                    },
                };
            });
        services.AddAuthorization();
        return services;
    }
}
