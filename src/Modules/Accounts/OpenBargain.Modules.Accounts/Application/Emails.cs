using FluentValidation;

namespace OpenBargain.Modules.Accounts.Application;

internal static class Emails
{
    public const int MaxLength = 256;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(MaxLength).WithMessage($"Email must be at most {MaxLength} characters.");
}
