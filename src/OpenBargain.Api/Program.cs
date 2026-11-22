using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenBargain.Api.Setup;
using OpenBargain.Modules.Accounts;

var builder = WebApplication.CreateBuilder(args);

// The one clock for the whole app (decisions.md #19). Tests replace it with FakeTimeProvider.
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddDatabase();
builder.Services.AddProblemDetails();
builder.Services.AddAppServiceForwardedHeaders();
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthRateLimiting();

builder.Services.AddAccountsModule();

var app = builder.Build();

// Each module applies its pending migrations (decisions.md #17).
if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
{
    await app.Services.MigrateAccountsDatabaseAsync();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Serves the built web app (src/web builds into wwwroot).
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoint();
app.MapAccountsEndpoints();

// Unknown /api routes stay 404; every other path is a client route for the web app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();
