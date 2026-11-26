using System.Threading.RateLimiting;
using OpenBargain.SharedKernel.Endpoints;

namespace OpenBargain.Api.Setup;

internal static class RateLimitingSetup
{
    // Policy "auth" (register, login, refresh): a fixed window per client IP, from RateLimiting:Auth.
    // Rejections are 429 ProblemDetails with code "rate_limited".
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, context =>
            {
                // Read per request, so tests can override the settings.
                var settings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("RateLimiting:Auth");
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GetValue("PermitLimit", 10),
                        Window = TimeSpan.FromSeconds(settings.GetValue("WindowSeconds", 60)),
                    });
            });
            options.OnRejected = async (context, ct) =>
            {
                var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many attempts. Try again in a minute.",
                        Extensions = { ["code"] = "rate_limited" },
                    },
                });
            };
        });
}
