using Xunit;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class StatusDisplayHelperTests
{
    public StatusDisplayHelperTests() => ViewModelTestSetup.EnsureAppStrings();

    [Theory]
    [InlineData("pl", "nowe", "Nowe")]
    [InlineData("pl", "w trakcie", "W toku")]
    [InlineData("pl", "zamknięte", "Zamknięte")]
    [InlineData("en", "nowe", "New")]
    [InlineData("en", "w trakcie", "In progress")]
    [InlineData("en", "zamknięte", "Closed")]
    public void ToDisplayStatus_ShouldMapApiValues(string culture, string apiStatus, string expected)
    {
        AppStrings.ApplyCulture(culture);
        Assert.Equal(expected, StatusDisplayHelper.ToDisplayStatus(apiStatus));
    }

    [Theory]
    [InlineData("pl", "Nowe", "nowe")]
    [InlineData("pl", "W toku", "w trakcie")]
    [InlineData("pl", "Zamknięte", "zamknięte")]
    [InlineData("en", "New", "nowe")]
    [InlineData("en", "In progress", "w trakcie")]
    [InlineData("en", "Closed", "zamknięte")]
    public void ToApiStatus_ShouldMapDisplayValues(string culture, string displayStatus, string expected)
    {
        AppStrings.ApplyCulture(culture);
        Assert.Equal(expected, StatusDisplayHelper.ToApiStatus(displayStatus));
    }

    [Theory]
    [InlineData("nowe", "nowe")]
    [InlineData("w trakcie", "w trakcie")]
    [InlineData("zamknięte", "zamknięte")]
    public void ToApiStatus_ShouldAcceptApiValuesUnchanged(string apiStatus, string expected)
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(expected, StatusDisplayHelper.ToApiStatus(apiStatus));
    }

    [Fact]
    public void ToDisplayStatus_Empty_ShouldReturnEmpty()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(string.Empty, StatusDisplayHelper.ToDisplayStatus(null));
        Assert.Equal(string.Empty, StatusDisplayHelper.ToDisplayStatus("   "));
    }

    [Fact]
    public void ToApiStatus_UnknownDisplay_ShouldDefaultToNowe()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("nowe", StatusDisplayHelper.ToApiStatus("nieznany"));
    }

    [Fact]
    public void RoundTrip_ApiToDisplayToApi_ShouldPreserveApiValue()
    {
        AppStrings.ApplyCulture("pl");
        const string api = "w trakcie";
        var display = StatusDisplayHelper.ToDisplayStatus(api);
        var back = StatusDisplayHelper.ToApiStatus(display);

        Assert.Equal("W toku", display);
        Assert.Equal(api, back);
    }

    [Theory]
    [InlineData("pl", "Closed", "zamknięte")]
    [InlineData("en", "Zamknięte", "zamknięte")]
    public void ToApiStatus_ShouldResolveLabelsFromAnyCulture(string currentCulture, string label, string expected)
    {
        AppStrings.ApplyCulture(currentCulture);
        Assert.Equal(expected, StatusDisplayHelper.ToApiStatus(label));
    }

    [Fact]
    public void ToDisplayStatus_EnglishApiAlias_ShouldUseCurrentCultureLabel()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("Zamknięte", StatusDisplayHelper.ToDisplayStatus("closed"));

        AppStrings.ApplyCulture("en");
        Assert.Equal("Closed", StatusDisplayHelper.ToDisplayStatus("closed"));
    }

    [Theory]
    [InlineData("nowe", "ticket-badge ticket-badge-status-new")]
    [InlineData("Closed", "ticket-badge ticket-badge-status-closed")]
    [InlineData("w trakcie", "ticket-badge ticket-badge-status-progress")]
    public void GetStatusBadgeClasses_ShouldUseNormalizedApiValue(string input, string expectedClasses)
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(expectedClasses, StatusDisplayHelper.GetStatusBadgeClasses(input));
    }
}
