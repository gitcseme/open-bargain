using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using static OpenBargain.IntegrationTests.AuthApi;

namespace OpenBargain.IntegrationTests;

public class RefreshTokenTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // Cookies are handled by hand (see AuthApi), so the client must not keep its own.
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task Login_sets_a_secure_http_only_strict_cookie_scoped_to_api_auth()
    {
        var user = NewUserData();
        var register = await _client.RegisterAsync(user);

        var login = await _client.LoginAsync(user.Email, user.Password);

        Assert.Null(register.RefreshSetCookie());
        var cookie = login.RefreshSetCookie()!.ToLowerInvariant();
        Assert.Contains("; httponly", cookie);
        Assert.Contains("; secure", cookie);
        Assert.Contains("; samesite=strict", cookie);
        Assert.Contains("; path=/api/auth", cookie);
        Assert.Contains("; expires=", cookie);
    }

    [Fact]
    public async Task Refresh_returns_a_new_access_token_and_a_new_cookie()
    {
        var (token, user) = await LogInAsync();

        var refresh = await _client.RefreshAsync(token);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.NotEqual(token, refresh.RefreshToken());
        var auth = await refresh.Content.ReadFromJsonAsync<AuthBody>();
        Assert.Equal(user.Email, auth!.User.Email);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetMeAsync(auth.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task A_rotated_token_no_longer_works()
    {
        var (token, _) = await LogInAsync();
        (await _client.RefreshAsync(token)).EnsureSuccessStatusCode();

        var again = await _client.RefreshAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);
        Assert.Equal("invalid_refresh_token", await again.ProblemCodeAsync());
        AssertCookieCleared(again);
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_all_the_users_tokens()
    {
        var (tokenA, _) = await LogInAsync();
        var tokenB = (await _client.RefreshAsync(tokenA)).RefreshToken();

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(tokenA)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(tokenB)).StatusCode);
    }

    [Fact]
    public async Task Parallel_refreshes_with_one_token_let_at_most_one_succeed()
    {
        var (token, _) = await LogInAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _client.RefreshAsync(token)));

        Assert.True(responses.Count(r => r.StatusCode == HttpStatusCode.OK) <= 1);
        // The losers count as reuse, so whatever the winner got is revoked as well.
        foreach (var winner in responses.Where(r => r.StatusCode == HttpStatusCode.OK))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(winner.RefreshToken())).StatusCode);
        }
    }

    [Fact]
    public async Task A_token_expires_after_7_days()
    {
        var (token, _) = await LogInAsync();

        factory.Time.Advance(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));

        var refresh = await _client.RefreshAsync(token);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        AssertCookieCleared(refresh);
    }

    [Fact]
    public async Task Each_refresh_extends_the_session_by_7_days()
    {
        var (token, _) = await LogInAsync();

        factory.Time.Advance(TimeSpan.FromDays(6));
        var next = (await _client.RefreshAsync(token)).RefreshToken();
        factory.Time.Advance(TimeSpan.FromDays(6));

        Assert.Equal(HttpStatusCode.OK, (await _client.RefreshAsync(next)).StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_token_and_clears_the_cookie()
    {
        var (token, _) = await LogInAsync();

        var logout = await _client.LogoutAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        AssertCookieCleared(logout);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(token)).StatusCode);
    }

    [Fact]
    public async Task Logout_without_a_cookie_returns_204()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _client.LogoutAsync(refreshToken: null)).StatusCode);
    }

    [Fact]
    public async Task Refresh_without_a_cookie_or_with_an_unknown_token_returns_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(refreshToken: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync("not-a-real-token")).StatusCode);
    }

    [Fact]
    public async Task Refresh_of_a_suspended_user_returns_401_and_revokes_all_their_tokens()
    {
        var user = NewUserData();
        var registered = await (await _client.RegisterAsync(user)).Content.ReadFromJsonAsync<UserBody>();
        var first = (await _client.LoginAsync(user.Email, user.Password)).RefreshToken();
        var second = (await _client.LoginAsync(user.Email, user.Password)).RefreshToken();
        await factory.SuspendAsync(registered!.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(first)).StatusCode);

        // Lift the suspension: the other session must still be gone.
        await factory.UnsuspendAsync(registered.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.RefreshAsync(second)).StatusCode);
    }

    private async Task<(string RefreshToken, NewUser User)> LogInAsync()
    {
        var user = NewUserData();
        (await _client.RegisterAsync(user)).EnsureSuccessStatusCode();
        var login = await _client.LoginAsync(user.Email, user.Password);
        login.EnsureSuccessStatusCode();
        return (login.RefreshToken(), user);
    }

    private static void AssertCookieCleared(HttpResponseMessage response)
    {
        var cookie = response.RefreshSetCookie()!.ToLowerInvariant();
        Assert.StartsWith("ob_refresh=;", cookie);
        Assert.Contains("expires=thu, 01 jan 1970", cookie);
        Assert.Contains("; path=/api/auth", cookie);
    }
}
