namespace OpenBargain.Modules.Accounts.Application;

internal sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

internal interface IAccessTokenIssuer
{
    AccessToken Issue(UserAccount user);
}
