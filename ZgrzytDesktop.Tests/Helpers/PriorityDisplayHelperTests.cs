using Xunit;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class PriorityDisplayHelperTests
{
    public PriorityDisplayHelperTests() => ViewModelTestSetup.EnsureAppStrings();

    [Theory]
    [InlineData("pl", "niski", "Niski")]
    [InlineData("pl", "średni", "Średni")]
    [InlineData("pl", "wysoki", "Wysoki")]
    [InlineData("en", "niski", "Low")]
    [InlineData("en", "średni", "Medium")]
    [InlineData("en", "wysoki", "High")]
    public void ToDisplayPriority_ShouldMapApiValues(string culture, string apiPriority, string expected)
    {
        AppStrings.ApplyCulture(culture);
        Assert.Equal(expected, PriorityDisplayHelper.ToDisplayPriority(apiPriority));
    }

    [Theory]
    [InlineData("pl", "Low", "niski")]
    [InlineData("en", "Niski", "niski")]
    public void ToApiPriority_ShouldResolveLabelsFromAnyCulture(string currentCulture, string label, string expected)
    {
        AppStrings.ApplyCulture(currentCulture);
        Assert.Equal(expected, PriorityDisplayHelper.ToApiPriority(label));
    }

    [Fact]
    public void ToDisplayPriority_EnglishApiAlias_ShouldUseCurrentCultureLabel()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("Niski", PriorityDisplayHelper.ToDisplayPriority("low"));

        AppStrings.ApplyCulture("en");
        Assert.Equal("Low", PriorityDisplayHelper.ToDisplayPriority("low"));
    }

    [Theory]
    [InlineData("niski", "ticket-badge ticket-badge-priority-low")]
    [InlineData("Low", "ticket-badge ticket-badge-priority-low")]
    [InlineData("wysoki", "ticket-badge ticket-badge-priority-high")]
    public void GetPriorityBadgeClasses_ShouldUseNormalizedApiValue(string input, string expectedClasses)
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(expectedClasses, PriorityDisplayHelper.GetPriorityBadgeClasses(input));
    }
}
