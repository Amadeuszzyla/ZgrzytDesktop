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
    public void IsHtmlContentType_TextHtml_ShouldBeTrue()
    {
        Assert.True(ApiErrorSanitizer.IsHtmlContentType("text/html; charset=UTF-8"));
    }

    [Fact]
    public void SanitizeApiErrorMessage_HtmlBodyWith500_ShouldReturnGeneralHtmlMessage()
    {
        const string html = "<html><body>500</body></html>";

        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(html, HttpStatusCode.InternalServerError);

        Assert.Equal("Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.", message);
    }

    [Fact]
    public void SanitizeApiErrorMessage_Forbidden_ShouldReturnShortMessage()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage("ignored", HttpStatusCode.Forbidden);

        Assert.Equal("Brak uprawnień do wykonania tej operacji.", message);
    }

    [Fact]
    public void SanitizeApiErrorMessage_Unauthorized_ShouldReturnSessionMessage()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(null, HttpStatusCode.Unauthorized);

        Assert.Equal("Sesja wygasła albo użytkownik nie jest zalogowany.", message);
    }

    [Fact]
    public void SanitizeApiErrorMessage_NotFound_ShouldReturnResourceMessage()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage("ignored", HttpStatusCode.NotFound);

        Assert.Equal("Nie znaleziono zasobu lub endpoint nie istnieje.", message);
    }

    [Fact]
    public void SanitizeApiErrorMessage_InternalServerError_ShouldReturnServerErrorMessage()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage("{\"error\":\"db\"}", HttpStatusCode.InternalServerError);

        Assert.Equal("Błąd serwera. Spróbuj ponownie później.", message);
    }

    [Fact]
    public void SanitizeApiErrorMessage_UnprocessableEntity_ShouldReturnValidationMessages()
    {
        const string json = """
            {
              "message": "The given data was invalid.",
              "errors": {
                "login": ["Pole login jest wymagane."],
                "password": ["Hasło jest za krótkie."]
              }
            }
            """;

        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(json, HttpStatusCode.UnprocessableEntity);

        Assert.Contains("login:", message, StringComparison.Ordinal);
        Assert.Contains("password:", message, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeForDisplay_ShouldDelegateToSanitizeApiErrorMessage()
    {
        var message = ApiErrorSanitizer.SanitizeForDisplay("ignored", HttpStatusCode.Forbidden);

        Assert.Equal("Brak uprawnień do wykonania tej operacji.", message);
    }
}
