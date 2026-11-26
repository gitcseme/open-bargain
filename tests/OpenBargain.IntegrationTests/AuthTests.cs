using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using static OpenBargain.IntegrationTests.AuthApi;

namespace OpenBargain.IntegrationTests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_then_login_then_me_returns_the_same_user()
    {
        var user = NewUserData();

        var register = await _client.RegisterAsync(user);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal("/api/me", register.Headers.Location?.OriginalString);
        var registered = await register.Content.ReadFromJsonAsync<UserBody>();
        Assert.Equal(user.Email, registered!.Email);
        Assert.Equal(user.DisplayName, registered.DisplayName);
        Assert.Null(registered.AvatarUrl);
        Assert.Equal("User", registered.Role);

        var login = await _client.LoginAsync(user.Email, user.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthBody>();
        Assert.Equal(registered, auth!.User);
        Assert.Equal(factory.Time.GetUtcNow().AddMinutes(15), auth.ExpiresAt, TimeSpan.FromSeconds(1));

        var me = await _client.GetMeAsync(auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(registered, await me.Content.ReadFromJsonAsync<UserBody>());
    }

    [Fact]
    public async Task Register_trims_the_display_name()
    {
        var user = NewUserData();

        var register = await _client.RegisterAsync(user with { DisplayName = $"  {user.DisplayName}  " });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(user.DisplayName, (await register.Content.ReadFromJsonAsync<UserBody>())!.DisplayName);
    }

    [Fact]
    public async Task Register_with_invalid_input_returns_field_errors()
    {
        var response = await _client.RegisterAsync(new NewUser("not-an-email", "short", " x "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["displayName", "email", "password"], errors.Select(e => e.Key).Order());
    }

    [Fact]
    public async Task Register_with_a_taken_email_returns_409_email_taken()
    {
        var user = NewUserData();
        (await _client.RegisterAsync(user)).EnsureSuccessStatusCode();

        var response = await _client.RegisterAsync(NewUserData() with { Email = user.Email.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("email_taken", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Register_with_a_taken_display_name_in_another_case_returns_409_display_name_taken()
    {
        var user = NewUserData();
        (await _client.RegisterAsync(user)).EnsureSuccessStatusCode();

        var response = await _client.RegisterAsync(NewUserData() with { DisplayName = user.DisplayName.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("display_name_taken", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Login_with_a_wrong_password_or_unknown_email_returns_the_same_401()
    {
        var user = NewUserData();
        (await _client.RegisterAsync(user)).EnsureSuccessStatusCode();

        var wrongPassword = await _client.LoginAsync(user.Email, "wrong password");
        var unknownEmail = await _client.LoginAsync($"nobody-{Guid.NewGuid():N}@example.com", user.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var wrongPasswordBody = await wrongPassword.ProblemWithoutTraceIdAsync();
        Assert.Contains("\"code\":\"invalid_credentials\"", wrongPasswordBody);
        Assert.Equal(wrongPasswordBody, await unknownEmail.ProblemWithoutTraceIdAsync());
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_for_15_minutes()
    {
        var user = NewUserData();
        (await _client.RegisterAsync(user)).EnsureSuccessStatusCode();

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(user.Email, "wrong password")).StatusCode);
        }

        var locked = await _client.LoginAsync(user.Email, user.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal("invalid_credentials", await locked.ProblemCodeAsync());

        factory.Time.Advance(TimeSpan.FromMinutes(15));

        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(user.Email, user.Password)).StatusCode);
    }

    [Fact]
    public async Task Login_of_a_suspended_user_returns_401_invalid_credentials()
    {
        var user = NewUserData();
        var registered = await (await _client.RegisterAsync(user)).Content.ReadFromJsonAsync<UserBody>();
        await factory.SuspendAsync(registered!.Id);

        var response = await _client.LoginAsync(user.Email, user.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", await response.ProblemCodeAsync());
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        var response = await _client.GetMeAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_an_expired_token_returns_401()
    {
        var (_, auth) = await _client.SignUpAsync();

        factory.Time.Advance(TimeSpan.FromMinutes(16));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetMeAsync(auth.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Me_of_a_suspended_user_returns_401()
    {
        var (_, auth) = await _client.SignUpAsync();
        await factory.SuspendAsync(auth.User.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetMeAsync(auth.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Too_many_auth_requests_return_429_rate_limited()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:PermitLimit", "2"))
            .CreateClient();
        var user = NewUserData();

        await client.LoginAsync(user.Email, user.Password);
        await client.LoginAsync(user.Email, user.Password);
        var third = await client.LoginAsync(user.Email, user.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal("rate_limited", await third.ProblemCodeAsync());
    }
}
