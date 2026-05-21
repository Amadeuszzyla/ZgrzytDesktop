using System.Net;
using Xunit;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class ApiServiceTests
{
    [Fact]
    public async Task GetAsync_WhenResponseIsHtml_ShouldThrowSanitizedMessageWithoutHtml()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueHtml(HttpStatusCode.InternalServerError,
                "<!DOCTYPE html><html><head><title>Laravel</title></head><body>500</body></html>");

            var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetAsync<object>("tickets"));

            Assert.DoesNotContain("<html", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Laravel", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(
                ex.Message.Contains("stronę błędu", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("Błąd serwera", StringComparison.OrdinalIgnoreCase),
                $"Unexpected message: {ex.Message}");
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetAsync_WhenForbidden_ShouldThrowForbiddenMessage()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.Forbidden, """{ "message": "Forbidden" }""");

            var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetAsync<object>("tickets"));

            Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
            Assert.Equal("Brak uprawnień do wykonania tej operacji.", ex.Message);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
