using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenBargain.Modules.Accounts.Infrastructure;

// Lets `dotnet ef migrations add` build the context without starting the app.
// Adding a migration never connects, so the connection string is only a placeholder.
internal sealed class AccountsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AccountsDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", AccountsDbContext.ConfigureNpgsql)
            .Options);
}
