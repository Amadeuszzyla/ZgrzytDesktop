using System.Net;
using Xunit;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class ApiServiceTests
{
    [Theory]
    [InlineData("/api/login")]
    [InlineData("api/login")]
    public async Task PostAsync_LoginEndpoint_ShouldComposeUrlWithoutDuplicateApiPrefix(string endpoint)
    {
        var handler = new MockHttpMessageHandler();
        var api = new ApiService(handler, ApiDefaults.ProductionApiBaseUrl);
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "{}");

            await api.PostAsync<object, object>(endpoint, new { });

            var requestUri = handler.Requests[0].Uri!;
            Assert.Equal("/api/login", requestUri.AbsolutePath, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("/api/api/", requestUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // handler disposed with api? ApiService doesn't dispose handler in test ctor - ok
        }
    }

    [Fact]
    public void SetBaseAddress_ShouldNormalizeProductionUrl()
    {
        var handler = new MockHttpMessageHandler();
        var api = new ApiService(handler, "https://zgrzyt-api.onrender.com");

        Assert.Equal(ApiDefaults.ProductionApiBaseUrl, api.CurrentApiBaseUrl);
    }

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
