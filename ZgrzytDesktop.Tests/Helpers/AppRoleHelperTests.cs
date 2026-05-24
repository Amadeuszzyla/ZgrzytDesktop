using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class AppRoleHelperTests
{
    [Theory]
    [InlineData("admin", true)]
    [InlineData("Admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("administrator", true)]
    [InlineData("Administrator", true)]
    [InlineData("it", false)]
    [InlineData("user", false)]
    public void IsAdmin_IsCaseInsensitive(string role, bool expected) =>
        Assert.Equal(expected, AppRoleHelper.IsAdmin(role));

    [Theory]
    [InlineData("it", true)]
    [InlineData("IT", true)]
    [InlineData("admin", false)]
    public void IsIt_IsCaseInsensitive(string role, bool expected) =>
        Assert.Equal(expected, AppRoleHelper.IsIt(role));

    [Theory]
    [InlineData(" admin ", true)]
    [InlineData(" administrator ", true)]
    [InlineData(" it ", false)]
    public void IsAdmin_TrimsWhitespace(string role, bool expected) =>
        Assert.Equal(expected, AppRoleHelper.IsAdmin(role));

    [Fact]
    public void IsIt_TrimsWhitespace() =>
        Assert.True(AppRoleHelper.IsIt(" it "));

    [Fact]
    public void IsAssignableStaffRole_IncludesAdminAndItVariants() =>
        Assert.True(AppRoleHelper.IsAssignableStaffRole(AppRoles.It) &&
                    AppRoleHelper.IsAssignableStaffRole("administrator") &&
                    AppRoleHelper.IsAssignableStaffRole(AppRoles.Admin));
}
