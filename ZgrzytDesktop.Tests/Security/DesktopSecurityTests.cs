using System.Net;
using System.Reflection;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
namespace ZgrzytDesktop.Tests.Security;

public class DesktopSecurityTests
{
    public DesktopSecurityTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task TokenStorage_DoesNotPersistPlaintextJwt()
    {
        var directory = CreateTempDirectory();
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.signature";

        try
        {
            var storage = new TokenStorage(directory);
            await storage.SaveTokenAsync(jwt);

            var raw = await File.ReadAllTextAsync(Path.Combine(directory, "token.txt"));
            Assert.DoesNotContain(jwt, raw, StringComparison.Ordinal);
            Assert.Equal(jwt, await storage.GetTokenAsync());
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task TicketCache_DoesNotPersistReadableTicketTitle()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalTicketCacheService(directory);
            await service.SaveTicketsAsync(
            [
                new Ticket
                {
                    Id = 1,
                    Title = "Secret Ticket Title",
                    Description = "Confidential details",
                    User = new User { Email = "user@example.com", Login = "user1" }
                }
            ]);

            var raw = await File.ReadAllTextAsync(Path.Combine(directory, "tickets-cache.json"));
            Assert.DoesNotContain("Secret Ticket Title", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("user@example.com", raw, StringComparison.Ordinal);

            var loaded = await service.LoadTicketsAsync();
            Assert.Equal("Secret Ticket Title", loaded[0].Title);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task AuditLog_DoesNotPersistReadableActionText()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new LocalAuditLogService(directory);
            await service.AddAsync(new AuditLogEntry
            {
                Action = "BanUser",
                UserLogin = "admin",
                Description = "Legacy readable description"
            });

            var raw = await File.ReadAllTextAsync(Path.Combine(directory, "audit-log.json"));
            Assert.DoesNotContain("BanUser", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("Legacy readable description", raw, StringComparison.Ordinal);

            var loaded = await service.LoadAsync();
            Assert.Equal("BanUser", loaded[0].Action);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void SettingsModel_DoesNotContainPasswordOrTokenProperties()
    {
        var secretProperties = typeof(AppSettings)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .Where(name =>
                name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("secret", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(secretProperties);
    }

    [Fact]
    public async Task SettingsService_PersistsReadablePlaintextJson()
    {
        var directory = CreateTempDirectory();

        try
        {
            var service = new SettingsService(directory);
            var settings = new AppSettings
            {
                UiCulture = "en",
                AutoLogoutEnabled = false,
                AutoLogoutTimeoutMinutes = 60
            };

            await service.SaveAsync(settings);

            var raw = await File.ReadAllTextAsync(Path.Combine(directory, "settings.json"));
            Assert.Contains("\"UiCulture\"", raw, StringComparison.Ordinal);
            Assert.Contains("en", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);

            var loaded = await service.LoadAsync();
            Assert.Equal("en", loaded.UiCulture);
            Assert.False(loaded.AutoLogoutEnabled);
            Assert.Equal(60, loaded.AutoLogoutTimeoutMinutes);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task AuthService_LogoutAsync_ClearsStoredToken()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """{"message":"Wylogowano"}""");

            var storage = new TokenStorage(tempDir);
            await storage.SaveTokenAsync("eyJhbGciOiJIUzI1NiJ9.test.signature");
            var auth = new AuthService(api, storage);

            await auth.LogoutAsync();

            Assert.False(File.Exists(Path.Combine(tempDir, "token.txt")));
            Assert.Null(await storage.GetTokenAsync());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Theory]
    [InlineData("http://zgrzyt-api.onrender.com/api/", "https://zgrzyt-api.onrender.com/api/")]
    [InlineData("http://127.0.0.1:9000/api/", "http://127.0.0.1:9000/api/")]
    [InlineData("http://localhost:9000/api/", "http://localhost:9000/api/")]
    public void ApiUrlSecurity_UpgradesHttpExceptLocalhost(string input, string expected)
    {
        var result = ApiUrlSecurityHelper.EnsureSecureApiBaseUrl(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DesktopAccess_ItAndAdminAllowed_UserDenied()
    {
        Assert.True(DesktopAccessHelper.IsDesktopAccessAllowed("admin"));
        Assert.True(DesktopAccessHelper.IsDesktopAccessAllowed("it"));
        Assert.False(DesktopAccessHelper.IsDesktopAccessAllowed("user"));
    }

    [Fact]
    public void ApiErrorSanitizer_StripsHtmlResponses()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(
            "<html><head><title>Laravel</title></head><body>Error</body></html>",
            HttpStatusCode.InternalServerError);

        Assert.Equal(AppStrings.Get("Api_HtmlResponse"), message);
        Assert.DoesNotContain("<html", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiErrorSanitizer_StripsStackTraces()
    {
        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(
            "System.Exception: boom\n   at System.Something.DoWork()",
            HttpStatusCode.InternalServerError);

        Assert.Equal(AppStrings.Get("Api_InternalServerError"), message);
        Assert.DoesNotContain("StackTrace", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SensitiveDataRedactor_RemovesBearerTokensFromText()
    {
        const string input = "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.abc.def";
        var redacted = SensitiveDataRedactor.Redact(input);

        Assert.DoesNotContain("eyJ", redacted, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void AppDataPaths_Root_IsUnderApplicationData()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assert.StartsWith(appData, AppDataPaths.RootDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("ZgrzytDesktop", AppDataPaths.RootDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
