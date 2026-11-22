using Microsoft.AspNetCore.Identity;
using OpenBargain.Modules.Accounts.Domain;

namespace OpenBargain.Modules.Accounts.Infrastructure;

// Identity's user (email, password hash, lockout) plus our profile fields.
internal sealed class AccountUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    // Set once avatar upload exists (M2).
    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsSuspended { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
