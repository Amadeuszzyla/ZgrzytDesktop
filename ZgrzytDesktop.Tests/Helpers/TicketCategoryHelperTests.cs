using Xunit;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketCategoryHelperTests
{
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
    public void FormatDescription_ShouldAddCategoryLine()
    {
        var result = TicketCategoryHelper.FormatDescription("Software", "Błąd aplikacji");

        Assert.StartsWith("Kategoria: Software", result, StringComparison.Ordinal);
        Assert.Contains("Błąd aplikacji", result, StringComparison.Ordinal);
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
