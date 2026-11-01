using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace OpenBargain.IntegrationTests;

public sealed class WebAppFallbackTests(ApiFactory factory) : IClassFixture<ApiFactory>, IDisposable
{
    // A stand-in for the built web app, so the test does not depend on npm run build.
    private readonly string _webRoot = Directory.CreateTempSubdirectory("openbargain-web").FullName;

    [Fact]
    public async Task Client_routes_return_the_web_app()
    {
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), "<div id=\"root\"></div>");
        var client = CreateClient();

        var response = await client.GetAsync("/rooms/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"root\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_api_routes_return_404_not_the_web_app()
    {
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), "<div id=\"root\"></div>");
        var client = CreateClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        factory.WithWebHostBuilder(builder => builder.UseWebRoot(_webRoot)).CreateClient();

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);
}
