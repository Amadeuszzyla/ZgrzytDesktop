using System.Globalization;
using ZgrzytDesktop.Converters;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class AdminUserMetaConverterTests
{
    private readonly AdminUserMetaConverter _converter = new();

    public AdminUserMetaConverterTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Convert_CultureEn_AdminMetaLines_AreEnglish()
    {
        AppStrings.ApplyCulture("en");

        Assert.Equal("Login: jan.kowalski", _converter.Convert("jan.kowalski", typeof(string), "login", CultureInfo.CurrentCulture));
        Assert.Equal("Email: jan@test.pl", _converter.Convert("jan@test.pl", typeof(string), "email", CultureInfo.CurrentCulture));
        Assert.Equal("Role: admin", _converter.Convert("admin", typeof(string), "role", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Convert_CulturePl_AdminMetaLines_ArePolish()
    {
        AppStrings.ApplyCulture("pl");

        Assert.Equal("Login: jan.kowalski", _converter.Convert("jan.kowalski", typeof(string), "login", CultureInfo.CurrentCulture));
        Assert.Equal("E-mail: jan@test.pl", _converter.Convert("jan@test.pl", typeof(string), "email", CultureInfo.CurrentCulture));
        Assert.Equal("Rola: admin", _converter.Convert("admin", typeof(string), "role", CultureInfo.CurrentCulture));
    }
}
