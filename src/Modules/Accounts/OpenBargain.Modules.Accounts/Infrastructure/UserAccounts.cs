using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.Modules.Accounts.Infrastructure;

internal sealed class UserAccounts(
    UserManager<AccountUser> userManager,
    AccountsDbContext db,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider time) : IUserAccounts
{
    public async Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        return user is null ? null : ToAccount(user);
    }

    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToAccount(user);
    }

    public Task<bool> IsDisplayNameTakenAsync(string normalizedDisplayName, Guid? exceptUserId, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.NormalizedDisplayName == normalizedDisplayName && u.Id != exceptUserId, ct);

    public async Task<Result<UserAccount>> CreateAsync(string email, string password, string displayName, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var user = new AccountUser
        {
            Id = Guid.CreateVersion7(now),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            NormalizedDisplayName = DisplayNames.Normalize(displayName),
            CreatedAt = now,
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException ex) when (UniqueViolation(ex) is { } index)
        {
            // A parallel registration took the email (user name = email) or the display name first.
            return index == "UserNameIndex" ? AccountsError.EmailTaken : AccountsError.DisplayNameTaken;
        }

        if (result.Succeeded)
        {
            return ToAccount(user);
        }

        var describer = new IdentityErrorDescriber();
        if (result.Errors.Any(e => e.Code is nameof(describer.DuplicateEmail) or nameof(describer.DuplicateUserName)))
        {
            return AccountsError.EmailTaken;
        }

        // Validation runs first, so any other Identity error is a bug.
        throw new InvalidOperationException(
            "Identity rejected the new user: " + string.Join(", ", result.Errors.Select(e => e.Code)));
    }

    // Lockout is counted here rather than by UserManager, because UserManager reads the system clock
    // and we use TimeProvider everywhere (decisions.md #19).
    public async Task<PasswordCheck> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");
        var now = time.GetUtcNow();

        if (user.LockoutEnabled && user.LockoutEnd > now)
        {
            return PasswordCheck.LockedOut;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            return PasswordCheck.Success;
        }

        var lockout = identityOptions.Value.Lockout;
        user.AccessFailedCount++;
        if (user.LockoutEnabled && user.AccessFailedCount >= lockout.MaxFailedAccessAttempts)
        {
            user.LockoutEnd = now + lockout.DefaultLockoutTimeSpan;
            user.AccessFailedCount = 0;
        }
        await db.SaveChangesAsync(ct);

        return PasswordCheck.WrongPassword;
    }

    public async Task ResetAccessFailedCountAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");
        if (user.AccessFailedCount != 0)
        {
            user.AccessFailedCount = 0;
            await db.SaveChangesAsync(ct);
        }
    }

    private static UserAccount ToAccount(AccountUser user) =>
        new(user.Id, user.Email!, user.DisplayName, user.AvatarUrl, user.Role, user.IsSuspended);

    // The name of the unique index a save violated, if that is why it failed.
    private static string? UniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg ? pg.ConstraintName : null;
}
