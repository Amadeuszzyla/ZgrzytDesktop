using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class DesktopAccessHelperTests
{
    [Theory]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("it", true)]
    [InlineData("IT", true)]
    [InlineData("administrator", true)]
    [InlineData("user", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsDesktopAccessAllowed_MatchesExpectedRoles(string? role, bool expected) =>
        Assert.Equal(expected, DesktopAccessHelper.IsDesktopAccessAllowed(role));

    [Fact]
    public void IsDesktopAccessAllowed_UsesAppRoleConstants() =>
        Assert.True(DesktopAccessHelper.IsDesktopAccessAllowed(AppRoles.Admin));
}
