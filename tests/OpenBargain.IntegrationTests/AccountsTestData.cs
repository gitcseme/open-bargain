using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBargain.Modules.Accounts.Infrastructure;

namespace OpenBargain.IntegrationTests;

// Direct database changes for states the API can't produce yet (suspension arrives in M9).
internal static class AccountsTestData
{
    public static Task SuspendAsync(this ApiFactory factory, Guid userId) => SetSuspendedAsync(factory, userId, true);

    public static Task UnsuspendAsync(this ApiFactory factory, Guid userId) => SetSuspendedAsync(factory, userId, false);

    private static async Task SetSuspendedAsync(ApiFactory factory, Guid userId, bool suspended)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsSuspended, suspended));
    }
}
