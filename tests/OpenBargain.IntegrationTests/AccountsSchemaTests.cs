using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OpenBargain.IntegrationTests;

public class AccountsSchemaTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Startup_migrates_the_accounts_schema()
    {
        _ = factory.CreateClient(); // starts the app, which applies migrations
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();

        await using var command = dataSource.CreateCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'accounts' ORDER BY table_name COLLATE \"C\"");
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // Only Identity's user table plus this module's own migration history.
        Assert.Equal(["Users", "__EFMigrationsHistory"], tables);
    }
}
