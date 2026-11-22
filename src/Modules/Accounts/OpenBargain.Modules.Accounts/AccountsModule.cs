using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenBargain.Modules.Accounts.Application;
using OpenBargain.Modules.Accounts.Endpoints;
using OpenBargain.Modules.Accounts.Infrastructure;

namespace OpenBargain.Modules.Accounts;

// The only public type in the module: how the host wires it up.
public static class AccountsModule
{
    // Expects the host to register the shared NpgsqlDataSource and a TimeProvider.
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddDbContext<AccountsDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), AccountsDbContext.ConfigureNpgsql));

        services.AddIdentityCore<AccountUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                // UserName = email, so allow every character an email may contain.
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "";

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AccountsDbContext>();

        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section);
        services.AddValidatorsFromAssembly(typeof(AccountsModule).Assembly, includeInternalTypes: true);

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddScoped<RegisterUser>();
        services.AddScoped<LoginUser>();
        services.AddScoped<RefreshSession>();
        services.AddScoped<Logout>();
        services.AddScoped<GetMe>();

        return services;
    }

    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapMeEndpoints();
        return app;
    }

    public static async Task MigrateAccountsDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
