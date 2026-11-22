using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenBargain.Modules.Accounts.Application;
using OpenBargain.SharedKernel.Endpoints;

namespace OpenBargain.Modules.Accounts.Endpoints;

internal static class AuthEndpoints
{
    // The refresh token cookie. Path-scoped so the browser sends it only to /api/auth/*.
    public const string RefreshCookie = "ob_refresh";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .RequireRateLimiting(RateLimitPolicies.Auth);

        auth.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .RequireRateLimiting(RateLimitPolicies.Auth);

        auth.MapPost("/refresh", Refresh)
            .RequireRateLimiting(RateLimitPolicies.Auth);

        auth.MapPost("/logout", LogOut);
    }

    // Creates the user only: no token and no cookie.
    private static async Task<IResult> Register(RegisterRequest request, RegisterUser registerUser, CancellationToken ct)
    {
        var result = await registerUser.ExecuteAsync(request, ct);
        return result.IsSuccess ? Results.Created("/api/me", result.Value) : AccountsProblems.For(result.Error!.Value);
    }

    private static async Task<IResult> Login(LoginRequest request, LoginUser loginUser, HttpResponse response, CancellationToken ct)
    {
        var result = await loginUser.ExecuteAsync(request, ct);
        if (!result.IsSuccess)
        {
            return AccountsProblems.For(result.Error!.Value);
        }

        SetRefreshCookie(response, result.Value);
        return Results.Ok(result.Value.Auth);
    }

    private static async Task<IResult> Refresh(HttpRequest request, RefreshSession refreshSession, CancellationToken ct)
    {
        var result = await refreshSession.ExecuteAsync(request.Cookies[RefreshCookie], ct);
        if (!result.IsSuccess)
        {
            ClearRefreshCookie(request.HttpContext.Response);
            return AccountsProblems.For(result.Error!.Value);
        }

        SetRefreshCookie(request.HttpContext.Response, result.Value);
        return Results.Ok(result.Value.Auth);
    }

    private static async Task<IResult> LogOut(HttpRequest request, Logout logout, CancellationToken ct)
    {
        await logout.ExecuteAsync(request.Cookies[RefreshCookie], ct);
        ClearRefreshCookie(request.HttpContext.Response);
        return Results.NoContent();
    }

    private static CookieOptions RefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
    };

    private static void SetRefreshCookie(HttpResponse response, Session session)
    {
        var options = RefreshCookieOptions();
        options.Expires = session.RefreshTokenExpiresAt;
        response.Cookies.Append(RefreshCookie, session.RefreshToken, options);
    }

    // Same attributes with an expiry in the past.
    private static void ClearRefreshCookie(HttpResponse response) =>
        response.Cookies.Delete(RefreshCookie, RefreshCookieOptions());
}
