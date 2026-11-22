using FluentValidation;

namespace OpenBargain.Modules.Accounts.Application;

internal static class DisplayNames
{
    public const int MinLength = 2;
    public const int MaxLength = 40;

    // Display names are unique ignoring case and surrounding spaces.
    public static string Normalize(string displayName) => displayName.Trim().ToUpperInvariant();

    // Shared by register and profile update: 2–40 characters after trimming.
    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotNull().WithMessage("Display name is required.")
            .Must(name => name is null || name.Trim().Length is >= MinLength and <= MaxLength)
            .WithMessage($"Display name must be {MinLength}–{MaxLength} characters.");
}
