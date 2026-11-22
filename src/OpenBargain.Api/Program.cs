using Npgsql;
using OpenBargain.Modules.Accounts;

var builder = WebApplication.CreateBuilder(args);

// Created on first use so tests can supply their own connection string.
// Shared by every module's DbContext.
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
    return NpgsqlDataSource.Create(connectionString);
});

builder.Services.AddAccountsModule();

var app = builder.Build();

// Each module applies its pending migrations (decisions.md #17).
if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
{
    await app.Services.MigrateAccountsDatabaseAsync();
}

// Serves the built web app (src/web builds into wwwroot).
app.UseStaticFiles();

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

// Unknown /api routes stay 404; every other path is a client route for the web app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();
