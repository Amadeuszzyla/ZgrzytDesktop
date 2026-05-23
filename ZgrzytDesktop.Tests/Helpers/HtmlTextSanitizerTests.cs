using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class HtmlTextSanitizerTests
{
    [Fact]
    public void ToPlainText_RemovesParagraphTags()
    {
        var result = HtmlTextSanitizer.ToPlainText("<p>Can you see me?</p>");

        Assert.Equal("Can you see me?", result);
    }

    [Fact]
    public void ToPlainText_DecodesHtmlEntities()
    {
        var result = HtmlTextSanitizer.ToPlainText("Tom &amp; Jerry&nbsp;now");

        Assert.Equal("Tom & Jerry now", result);
    }

    [Fact]
    public void ToPlainText_RemovesScriptContent()
    {
        var result = HtmlTextSanitizer.ToPlainText(
            "<p>Hello</p><script>alert('x')</script><p>World</p>");

        Assert.Equal("Hello World", result);
        Assert.DoesNotContain("alert", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_NullOrEmpty_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, HtmlTextSanitizer.ToPlainText(input));
    }
}
