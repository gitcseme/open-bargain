using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace OpenBargain.IntegrationTests;

public class SigningKeyStartupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("")]
    [InlineData("only-31-bytes-long-signing-key!")]
    public void App_refuses_to_start_without_a_valid_signing_key(string signingKey)
    {
        using var app = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Jwt:SigningKey", signingKey)
            .UseSetting("Database:MigrateOnStartup", "false"));

        var ex = Assert.Throws<OptionsValidationException>(() => app.CreateClient());

        Assert.Contains("Jwt:SigningKey must be set to at least 32 bytes", ex.Message);
    }
}
