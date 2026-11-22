using Npgsql;

namespace OpenBargain.Api.Setup;

internal static class DatabaseSetup
{
    // One NpgsqlDataSource, shared by every module's DbContext. Created on first use,
    // so tests can supply their own connection string.
    public static IServiceCollection AddDatabase(this IServiceCollection services) =>
        services.AddSingleton(sp =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
            return NpgsqlDataSource.Create(connectionString);
        });

    // GET /api/health: 200 when the database answers, 503 with a body when it doesn't.
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (IServiceProvider services, ILogger<Program> logger, CancellationToken ct) =>
        {
            try
            {
                var dataSource = services.GetRequiredService<NpgsqlDataSource>();
                await using var command = dataSource.CreateCommand("SELECT 1");
                await command.ExecuteScalarAsync(ct);
                return Results.Ok(new { status = "ok", database = "ok" });
            }
            catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
            {
                logger.LogWarning(ex, "Database health check failed");
                return Results.Json(new { status = "degraded", database = "unavailable" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });
        return app;
    }
}
