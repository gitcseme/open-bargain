using Microsoft.AspNetCore.HttpOverrides;

namespace OpenBargain.Api.Setup;

internal static class ForwardedHeadersSetup
{
    // App Service terminates TLS in front of the app; trust its forwarded client IP and scheme.
    public static IServiceCollection AddAppServiceForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
}
