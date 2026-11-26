using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenBargain.Modules.Accounts.Application;
using OpenBargain.SharedKernel.Endpoints;

namespace OpenBargain.Modules.Accounts.Endpoints;

internal static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", GetMe).RequireAuthorization();

        app.MapPut("/api/me", UpdateMe)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<UpdateProfileRequest>>();
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal principal, GetMe getMe, CancellationToken ct)
    {
        var result = await getMe.ExecuteAsync(principal.GetUserId(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : AccountsProblems.For(result.Error!.Value);
    }

    private static async Task<IResult> UpdateMe(
        UpdateProfileRequest request, ClaimsPrincipal principal, UpdateProfile updateProfile, CancellationToken ct)
    {
        var result = await updateProfile.ExecuteAsync(principal.GetUserId(), request, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : AccountsProblems.For(result.Error!.Value);
    }
}
