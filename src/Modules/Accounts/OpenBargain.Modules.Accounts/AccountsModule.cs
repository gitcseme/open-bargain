using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenBargain.Modules.Accounts.Infrastructure;

namespace OpenBargain.Modules.Accounts;

// The only public type in the module: how the host wires it up.
public static class AccountsModule
{
    // Expects the host to register the shared NpgsqlDataSource.
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddDbContext<AccountsDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), AccountsDbContext.ConfigureNpgsql));

        return services;
    }

    public static async Task MigrateAccountsDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
