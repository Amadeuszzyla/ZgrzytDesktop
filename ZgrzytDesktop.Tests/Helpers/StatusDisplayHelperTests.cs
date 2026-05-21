using Xunit;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class StatusDisplayHelperTests
{
    [Theory]
    [InlineData("nowe", "Nowe")]
    [InlineData("w trakcie", "W toku")]
    [InlineData("zamknięte", "Rozwiązane")]
    [InlineData("NOWE", "Nowe")]
    public void ToDisplayStatus_ShouldMapApiValues(string apiStatus, string expected)
    {
        Assert.Equal(expected, StatusDisplayHelper.ToDisplayStatus(apiStatus));
    }

    [Theory]
    [InlineData("Nowe", "nowe")]
    [InlineData("W toku", "w trakcie")]
    [InlineData("Rozwiązane", "zamknięte")]
    public void ToApiStatus_ShouldMapDisplayValues(string displayStatus, string expected)
    {
        Assert.Equal(expected, StatusDisplayHelper.ToApiStatus(displayStatus));
    }

    [Theory]
    [InlineData("nowe", "nowe")]
    [InlineData("w trakcie", "w trakcie")]
    [InlineData("zamknięte", "zamknięte")]
    public void ToApiStatus_ShouldAcceptApiValuesUnchanged(string apiStatus, string expected)
    {
        Assert.Equal(expected, StatusDisplayHelper.ToApiStatus(apiStatus));
    }

    [Fact]
    public void ToDisplayStatus_Empty_ShouldReturnEmpty()
    {
        Assert.Equal(string.Empty, StatusDisplayHelper.ToDisplayStatus(null));
        Assert.Equal(string.Empty, StatusDisplayHelper.ToDisplayStatus("   "));
    }

    [Fact]
    public void ToApiStatus_UnknownDisplay_ShouldDefaultToNowe()
    {
        Assert.Equal("nowe", StatusDisplayHelper.ToApiStatus("nieznany"));
    }

    [Fact]
    public void RoundTrip_ApiToDisplayToApi_ShouldPreserveApiValue()
    {
        const string api = "w trakcie";
        var display = StatusDisplayHelper.ToDisplayStatus(api);
        var back = StatusDisplayHelper.ToApiStatus(display);

        Assert.Equal("W toku", display);
        Assert.Equal(api, back);
    }
}
