using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace OpenBargain.Modules.Accounts.Infrastructure;

internal sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options)
    : IdentityUserContext<AccountUser, Guid>(options)
{
    public const string Schema = "accounts";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Keeps this module's migration history in its own schema.
    public static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        // One IEntityTypeConfiguration per entity, in Infrastructure/Configurations.
        builder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);

        // We use only Identity's user table: no external logins, claims or stored tokens.
        builder.Ignore<IdentityUserClaim<Guid>>();
        builder.Ignore<IdentityUserLogin<Guid>>();
        builder.Ignore<IdentityUserToken<Guid>>();
    }
}
