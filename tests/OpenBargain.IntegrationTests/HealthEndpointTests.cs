using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace OpenBargain.IntegrationTests;

public class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_returns_ok_when_database_is_reachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal(new HealthResponse("ok", "ok"), body);
    }

    [Fact]
    public async Task Health_returns_503_when_database_is_unreachable()
    {
        // Port 1 on localhost refuses connections immediately.
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=1;Database=x;Username=x;Password=x;Timeout=2"))
            .CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal(new HealthResponse("degraded", "unavailable"), body);
    }

    private sealed record HealthResponse(string Status, string Database);
}
