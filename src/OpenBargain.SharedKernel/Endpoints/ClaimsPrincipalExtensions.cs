using System.Security.Claims;

namespace OpenBargain.SharedKernel.Endpoints;

public static class ClaimsPrincipalExtensions
{
    // The access token's "sub" claim (inbound claim mapping is off).
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var id)
            ? id
            : throw new InvalidOperationException("The user has no valid 'sub' claim.");
}
