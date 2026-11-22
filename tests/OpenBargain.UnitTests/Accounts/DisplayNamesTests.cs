using OpenBargain.Modules.Accounts.Application;

namespace OpenBargain.UnitTests.Accounts;

public class DisplayNamesTests
{
    [Theory]
    [InlineData("Alice", "ALICE")]
    [InlineData("  alice smith ", "ALICE SMITH")]
    [InlineData("ÅSE", "ÅSE")]
    [InlineData("åse", "ÅSE")]
    public void Normalize_trims_and_upper_cases(string input, string expected) =>
        Assert.Equal(expected, DisplayNames.Normalize(input));
}
