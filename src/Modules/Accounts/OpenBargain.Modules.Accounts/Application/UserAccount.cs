using OpenBargain.Modules.Accounts.Domain;

namespace OpenBargain.Modules.Accounts.Application;

// What the use cases need to know about a user, without depending on Identity's types.
internal sealed record UserAccount(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    UserRole Role,
    bool IsSuspended);
