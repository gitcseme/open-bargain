namespace OpenBargain.SharedKernel.Endpoints;

// Rate limit policy names. The host defines the policies; modules apply them with RequireRateLimiting.
public static class RateLimitPolicies
{
    // Register, login and refresh: a fixed window per client IP (RateLimiting:Auth).
    public const string Auth = "auth";
}
