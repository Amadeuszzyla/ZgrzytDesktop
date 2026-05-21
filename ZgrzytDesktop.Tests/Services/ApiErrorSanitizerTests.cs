using System.Net;
using Xunit;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Services;

public class ApiErrorSanitizerTests
{
    [Fact]
    public void IsHtmlResponse_ShouldDetectLaravelPage()
    {
        const string html = "<!DOCTYPE html><html><head><title>Laravel</title></head><body>Error</body></html>";

        Assert.True(ApiErrorSanitizer.IsHtmlResponse(html));
    }

    [Fact]
    public void SanitizeApiErrorMessage_ShouldNotReturnRawHtml()
    {
        const string html = "<!DOCTYPE html><html><body>Server Error</body></html>";

        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(html, HttpStatusCode.InternalServerError);

        Assert.DoesNotContain("<html", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stronę błędu", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeApiErrorMessage_Forbidden_ShouldReturnShortMessage()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage("ignored", HttpStatusCode.Forbidden);

        Assert.Equal("Brak uprawnień do wykonania tej operacji.", message);
    }
}
