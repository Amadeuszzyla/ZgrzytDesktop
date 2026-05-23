using Xunit;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketCategoryHelperTests
{
    public TicketCategoryHelperTests() => ViewModelTestSetup.EnsureAppStrings();

    [Theory]
    [InlineData("Hardware")]
    [InlineData("Software")]
    [InlineData("Sieć")]
    public void ExtractCategory_ShouldReadFromTitlePrefix(string category)
    {
        var result = TicketCategoryHelper.ExtractCategory($"[{category}] Problem", string.Empty);

        Assert.Equal(category, result);
    }

    [Fact]
    public void ExtractCategory_NoCategory_ShouldReturnEmpty()
    {
        Assert.Equal(string.Empty, TicketCategoryHelper.ExtractCategory("Zwykły tytuł", "Opis bez linii kategorii"));
    }

    [Fact]
    public void ExtractCategory_ShouldReadFromDescriptionLine()
    {
        AppStrings.ApplyCulture("pl");
        var category = TicketCategoryHelper.ExtractCategory(
            "Tytuł",
            "Kategoria: Sieć\n\nBrak internetu");

        Assert.Equal("Sieć", category);
    }

    [Fact]
    public void FormatTitle_ShouldAddCategoryPrefix()
    {
        var result = TicketCategoryHelper.FormatTitle("Hardware", "Monitor nie działa");

        Assert.Equal("[Hardware] Monitor nie działa", result);
    }

    [Fact]
    public void FormatTitle_NoCategory_ShouldReturnTitleOnly()
    {
        Assert.Equal("Monitor", TicketCategoryHelper.FormatTitle(null, "Monitor"));
    }

    [Fact]
    public void FormatDescription_ShouldAddCategoryLineInPolish()
    {
        AppStrings.ApplyCulture("pl");
        var result = TicketCategoryHelper.FormatDescription("Software", "Błąd aplikacji");

        Assert.StartsWith("Kategoria: Software", result, StringComparison.Ordinal);
        Assert.Contains("Błąd aplikacji", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatDescription_ShouldAddCategoryLineInEnglish()
    {
        AppStrings.ApplyCulture("en");
        var result = TicketCategoryHelper.FormatDescription("Software", "Application error");

        Assert.StartsWith("Category: Software", result, StringComparison.Ordinal);
        Assert.Contains("Application error", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDisplayCategory_ShouldLocalizeLabels()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("Sieć", TicketCategoryHelper.ToDisplayCategory("Sieć"));

        AppStrings.ApplyCulture("en");
        Assert.Equal("Network", TicketCategoryHelper.ToDisplayCategory("Sieć"));
    }

    [Fact]
    public void StripTitleCategoryPrefix_ShouldRemoveBracketedCategory()
    {
        var title = TicketCategoryHelper.StripTitleCategoryPrefix("[Hardware] Drukarka");

        Assert.Equal("Drukarka", title);
    }

    [Fact]
    public void Categories_ShouldContainExpectedValues()
    {
        Assert.Equal(["Hardware", "Software", "Sieć"], TicketCategoryHelper.Categories);
    }
}
