using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenBargain.IntegrationTests;

// Small helpers for calling the auth API. Every user gets a unique email and display name,
// so tests can share one database.
internal static class AuthApi
{
    public const string Password = "correct horse battery";

    public sealed record NewUser(string Email, string Password, string DisplayName);

    public sealed record UserBody(Guid Id, string Email, string DisplayName, string? AvatarUrl, string Role);

    public sealed record AuthBody(string AccessToken, DateTimeOffset ExpiresAt, UserBody User);

    public static NewUser NewUserData()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new NewUser($"user-{suffix}@example.com", Password, $"User {suffix}");
    }

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, NewUser user) =>
        client.PostAsJsonAsync("/api/auth/register", user);

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    // Registers a new user and logs in.
    public static async Task<(NewUser User, AuthBody Auth)> SignUpAsync(this HttpClient client)
    {
        var user = NewUserData();
        (await client.RegisterAsync(user)).EnsureSuccessStatusCode();
        var login = await client.LoginAsync(user.Email, user.Password);
        login.EnsureSuccessStatusCode();
        return (user, (await login.Content.ReadFromJsonAsync<AuthBody>())!);
    }

    public static Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return client.SendAsync(request);
    }

    // The raw Set-Cookie header for ob_refresh, if the response sets or clears it.
    public static string? RefreshSetCookie(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.SingleOrDefault(c => c.StartsWith("ob_refresh=", StringComparison.Ordinal))
            : null;

    // The cookie value from the ob_refresh Set-Cookie header.
    public static string RefreshToken(this HttpResponseMessage response)
    {
        var header = response.RefreshSetCookie() ?? throw new InvalidOperationException("No ob_refresh cookie was set.");
        return header["ob_refresh=".Length..header.IndexOf(';')];
    }

    // Sends the cookie by hand, so each test controls exactly which token is presented.
    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string? refreshToken) =>
        client.SendAsync(WithRefreshCookie(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh"), refreshToken));

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client, string? refreshToken) =>
        client.SendAsync(WithRefreshCookie(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout"), refreshToken));

    private static HttpRequestMessage WithRefreshCookie(HttpRequestMessage request, string? refreshToken)
    {
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"ob_refresh={refreshToken}");
        }
        return request;
    }

    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        return body?["code"]?.GetValue<string>();
    }

    // The ProblemDetails body without its per-request traceId.
    public static async Task<string> ProblemWithoutTraceIdAsync(this HttpResponseMessage response)
    {
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        body.Remove("traceId");
        return body.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
