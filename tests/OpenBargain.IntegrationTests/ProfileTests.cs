using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using static OpenBargain.IntegrationTests.AuthApi;

namespace OpenBargain.IntegrationTests;

public class ProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Update_changes_the_display_name()
    {
        var (_, auth) = await _client.SignUpAsync();
        var newName = NewUserData().DisplayName;

        var update = await UpdateMeAsync(auth.AccessToken, $"  {newName} ");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<UserBody>();
        Assert.Equal(auth.User with { DisplayName = newName }, updated);
        var me = await (await _client.GetMeAsync(auth.AccessToken)).Content.ReadFromJsonAsync<UserBody>();
        Assert.Equal(newName, me!.DisplayName);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("   ")]
    [InlineData("this display name is far too long to be accepted")]
    public async Task Update_with_an_invalid_name_returns_400(string displayName)
    {
        var (_, auth) = await _client.SignUpAsync();

        var update = await UpdateMeAsync(auth.AccessToken, displayName);

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        var errors = (await update.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject();
        Assert.Equal(["displayName"], errors.Select(e => e.Key));
    }

    [Fact]
    public async Task Update_to_a_name_another_user_has_in_another_case_returns_409()
    {
        var (other, _) = await _client.SignUpAsync();
        var (_, auth) = await _client.SignUpAsync();

        var update = await UpdateMeAsync(auth.AccessToken, other.DisplayName.ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        Assert.Equal("display_name_taken", await update.ProblemCodeAsync());
    }

    [Fact]
    public async Task Changing_only_the_case_of_your_own_name_is_allowed()
    {
        var (user, auth) = await _client.SignUpAsync();

        var update = await UpdateMeAsync(auth.AccessToken, user.DisplayName.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(user.DisplayName.ToUpperInvariant(), (await update.Content.ReadFromJsonAsync<UserBody>())!.DisplayName);
    }

    [Fact]
    public async Task Update_without_a_token_returns_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await UpdateMeAsync(accessToken: null, "New name")).StatusCode);
    }

    private Task<HttpResponseMessage> UpdateMeAsync(string? accessToken, string displayName)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me") { Content = JsonContent.Create(new { displayName }) };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return _client.SendAsync(request);
    }
}
