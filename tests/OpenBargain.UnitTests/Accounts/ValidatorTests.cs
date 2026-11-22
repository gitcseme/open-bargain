using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.UnitTests.Accounts;

public class ValidatorTests
{
    private static readonly RegisterRequestValidator Register = new();
    private static readonly LoginRequestValidator Login = new();

    private static RegisterRequest ValidRegister() => new("ada@example.com", "12345678", "Ada");

    [Fact]
    public void A_valid_register_request_passes() => Assert.True(Register.Validate(ValidRegister()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    public void Register_rejects_invalid_emails(string email) =>
        AssertOnlyError(Register.Validate(ValidRegister() with { Email = email }), nameof(RegisterRequest.Email));

    [Fact]
    public void Register_rejects_emails_over_256_characters() =>
        AssertOnlyError(
            Register.Validate(ValidRegister() with { Email = new string('a', 245) + "@example.com" }),
            nameof(RegisterRequest.Email));

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Register_password_must_be_8_to_128_characters(int length, bool valid) =>
        Assert.Equal(valid, Register.Validate(ValidRegister() with { Password = new string('a', length) }).IsValid);

    [Fact]
    public void Register_password_has_no_composition_rules() =>
        Assert.True(Register.Validate(ValidRegister() with { Password = "aaaaaaaa" }).IsValid);

    [Theory]
    [InlineData("A", false)]
    [InlineData("  A  ", false)]
    [InlineData("Al", true)]
    [InlineData("  Al  ", true)]
    [InlineData("", false)]
    public void Register_display_name_must_be_2_to_40_characters_after_trimming(string name, bool valid) =>
        Assert.Equal(valid, Register.Validate(ValidRegister() with { DisplayName = name }).IsValid);

    [Fact]
    public void Register_display_name_of_40_characters_passes_and_41_fails()
    {
        Assert.True(Register.Validate(ValidRegister() with { DisplayName = new string('a', 40) }).IsValid);
        Assert.False(Register.Validate(ValidRegister() with { DisplayName = new string('a', 41) }).IsValid);
    }

    [Fact]
    public void Register_rejects_missing_fields()
    {
        var result = Register.Validate(new RegisterRequest(null!, null!, null!));

        Assert.Equal(
            [nameof(RegisterRequest.DisplayName), nameof(RegisterRequest.Email), nameof(RegisterRequest.Password)],
            result.Errors.Select(e => e.PropertyName).Distinct().Order());
    }

    [Fact]
    public void Login_requires_an_email_and_a_password()
    {
        Assert.True(Login.Validate(new LoginRequest("ada@example.com", "x")).IsValid);
        Assert.False(Login.Validate(new LoginRequest("ada@example.com", "")).IsValid);
        Assert.False(Login.Validate(new LoginRequest("nope", "x")).IsValid);
    }

    private static void AssertOnlyError(FluentValidation.Results.ValidationResult result, string property)
    {
        Assert.False(result.IsValid);
        Assert.All(result.Errors, e => Assert.Equal(property, e.PropertyName));
    }
}
