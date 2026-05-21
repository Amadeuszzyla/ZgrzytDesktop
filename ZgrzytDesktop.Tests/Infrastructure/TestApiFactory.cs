using ZgrzytDesktop.Services;
using ZgrzytDesktop.Storage;

namespace ZgrzytDesktop.Tests.Infrastructure;

internal static class TestApiFactory
{
    private const string BaseUrl = "http://127.0.0.1:9000/api/";

    public static (ApiService Api, MockHttpMessageHandler Handler, string TempDirectory) CreateApi()
    {
        var handler = new MockHttpMessageHandler();
        var api = new ApiService(handler, BaseUrl);
        var tempDir = Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return (api, handler, tempDir);
    }

    public static AuthService CreateAuth(ApiService api, string tempDir) =>
        new(api, new TokenStorage(tempDir));

    public static TicketService CreateTickets(ApiService api) => new(api);

    public static UserAdminService CreateUserAdmin(ApiService api) => new(api);

    public static void Cleanup(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignoruj problemy sprzątania katalogu tymczasowego w testach.
        }
    }

    public static string? LastRequestPath(MockHttpMessageHandler handler) =>
        handler.Requests.LastOrDefault()?.Uri?.AbsolutePath;

    public static string? LastRequestBody(MockHttpMessageHandler handler) =>
        handler.Requests.LastOrDefault()?.Body;
}
