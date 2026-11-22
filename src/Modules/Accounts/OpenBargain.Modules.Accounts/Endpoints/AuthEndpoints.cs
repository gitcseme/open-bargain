using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenBargain.Modules.Accounts.Application;
using OpenBargain.SharedKernel.Endpoints;

namespace OpenBargain.Modules.Accounts.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .RequireRateLimiting(RateLimitPolicies.Auth);

        auth.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .RequireRateLimiting(RateLimitPolicies.Auth);
    }

    private static async Task<IResult> Register(RegisterRequest request, RegisterUser registerUser, CancellationToken ct)
    {
        var result = await registerUser.ExecuteAsync(request, ct);
        return result.IsSuccess ? Results.Created("/api/me", result.Value) : AccountsProblems.For(result.Error!.Value);
    }

    private static async Task<IResult> Login(LoginRequest request, LoginUser loginUser, CancellationToken ct)
    {
        var result = await loginUser.ExecuteAsync(request, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : AccountsProblems.For(result.Error!.Value);
    }
}
