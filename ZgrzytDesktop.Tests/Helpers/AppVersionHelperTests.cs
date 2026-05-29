using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class AppVersionHelperTests
{
    [Theory]
    [InlineData("0.1.0-alpha", null, "v0.1.0-alpha")]
    [InlineData("1.2.3", "9.9.9", "v1.2.3")]
    [InlineData(null, "2.0.0", "v2.0.0")]
    [InlineData("v3.4.5-beta", null, "v3.4.5-beta")]
    [InlineData("1.0.0+abc1234", null, "v1.0.0")]
    [InlineData("", "4.5.6", "v4.5.6")]
    public void FormatDisplayVersion_FormatsExpected(
        string? informationalVersion,
        string? assemblyVersion,
        string expected) =>
        Assert.Equal(expected, AppVersionHelper.FormatDisplayVersion(informationalVersion, assemblyVersion));

    [Fact]
    public void DisplayVersion_ReadsFromApplicationAssembly() =>
        Assert.Equal("v0.1.0-alpha", AppVersionHelper.DisplayVersion);
}
