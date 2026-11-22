using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBargain.Modules.Accounts.Infrastructure;

namespace OpenBargain.IntegrationTests;

// Direct database changes for states the API can't produce yet (suspension arrives in M9).
internal static class AccountsTestData
{
    public static async Task SuspendAsync(this ApiFactory factory, Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsSuspended, true));
    }
}
