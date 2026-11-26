using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace OpenBargain.IntegrationTests;

// Runs the real app in memory against a throwaway PostgreSQL in Docker.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    // The app's clock. Starts at real time so tokens and the database agree; tests move it forward.
    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    public Task InitializeAsync() => _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-at-least-32-bytes");
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "1000");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Time));
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
