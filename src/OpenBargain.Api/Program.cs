using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenBargain.Modules.Accounts;
using OpenBargain.SharedKernel.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Created on first use so tests can supply their own connection string.
// Shared by every module's DbContext.
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
    return NpgsqlDataSource.Create(connectionString);
});

// The one clock for the whole app (decisions.md #19). Tests replace it with FakeTimeProvider.
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddProblemDetails();

// App Service terminates TLS in front of the app; trust its forwarded client IP and scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Accounts issues access tokens; the host validates them. Options are read when first used,
// so tests can override the settings.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration, TimeProvider>((options, config, time) =>
    {
        var clockSkew = TimeSpan.FromSeconds(30);
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey(config))),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = clockSkew,
            // Checks expiry against the app's TimeProvider rather than the system clock.
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = time.GetUtcNow().UtcDateTime;
                return (notBefore is null || notBefore.Value <= now + clockSkew)
                    && expires is not null && expires.Value > now - clockSkew;
            },
        };
    });
builder.Services.AddAuthorization();

// Register, login and refresh: a fixed window per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, context =>
    {
        var settings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("RateLimiting:Auth");
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.GetValue("PermitLimit", 10),
                Window = TimeSpan.FromSeconds(settings.GetValue("WindowSeconds", 60)),
            });
    });
    options.OnRejected = async (context, ct) =>
    {
        var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many attempts. Try again in a minute.",
                Extensions = { ["code"] = "rate_limited" },
            },
        });
    };
});

builder.Services.AddAccountsModule();

var app = builder.Build();

// Fail fast rather than issue tokens with a weak or missing key.
SigningKey(app.Configuration);

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

app.MapAccountsEndpoints();

// Unknown /api routes stay 404; every other path is a client route for the web app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();

static string SigningKey(IConfiguration config)
{
    var key = config["Jwt:SigningKey"];
    if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be set to at least 32 bytes (App Service setting Jwt__SigningKey).");
    }
    return key;
}
