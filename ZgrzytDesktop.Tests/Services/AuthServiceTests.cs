using System.Net;
using Xunit;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldSaveTokenAndReturnUser()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "access_token": "token-abc", "token_type": "Bearer", "role": "admin" }
                """);
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "id": 1, "login": "admin", "email": "a@test.pl", "role": "admin", "active": true, "ban": false }
                """);
            var auth = TestApiFactory.CreateAuth(api, tempDir);

            var user = await auth.LoginAsync("admin", "secret");

            Assert.NotNull(user);
            Assert.Equal("admin", user!.Login);
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("/api/login", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("/api/user", handler.Requests[1].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(await new TokenStorage(tempDir).GetTokenAsync());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ShouldReturnNull()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """{ "access_token": "", "token_type": "Bearer", "role": "user" }""");
            var auth = TestApiFactory.CreateAuth(api, tempDir);

            var user = await auth.LoginAsync("x", "y");

            Assert.Null(user);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallLogoutAndClearToken()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            var storage = new TokenStorage(tempDir);
            await storage.SaveTokenAsync("stored-token");
            api.SetToken("stored-token");

            handler.EnqueueJson(HttpStatusCode.OK, """{ "message": "Wylogowano" }""");
            var auth = TestApiFactory.CreateAuth(api, tempDir);

            await auth.LogoutAsync();

            Assert.EndsWith("/api/logout", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Null(await storage.GetTokenAsync());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldSaveNewToken()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "access_token": "new-token", "token_type": "Bearer", "role": "it" }
                """);
            var auth = TestApiFactory.CreateAuth(api, tempDir);

            var refreshed = await auth.RefreshTokenAsync();

            Assert.True(refreshed);
            Assert.EndsWith("/api/refresh", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("new-token", await new TokenStorage(tempDir).GetTokenAsync());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetCurrentUserAsync_On401_WithSuccessfulRefresh_ShouldRetryAndReturnUser()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            api.SetToken("old-token");
            var refreshCalled = false;
            api.TryRefreshSessionAsync = () =>
            {
                refreshCalled = true;
                api.SetToken("new-token");
                return Task.FromResult(true);
            };

            handler.EnqueueJson(HttpStatusCode.Unauthorized, """{ "message": "Unauthenticated" }""");
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "id": 3, "login": "it", "email": "it@test.pl", "role": "it", "active": true, "ban": false }
                """);

            var auth = TestApiFactory.CreateAuth(api, tempDir);
            var user = await auth.GetCurrentUserAsync();

            Assert.True(refreshCalled);
            Assert.NotNull(user);
            Assert.Equal("it", user!.Login);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetCurrentUserAsync_On401_WhenRefreshFails_ShouldThrowUnauthorizedMessage()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            api.SetToken("expired");
            api.TryRefreshSessionAsync = () => Task.FromResult(false);
            var expiredCalled = false;
            api.OnSessionExpiredAsync = () =>
            {
                expiredCalled = true;
                return Task.CompletedTask;
            };

            handler.EnqueueJson(HttpStatusCode.Unauthorized, """{ "message": "Unauthenticated" }""");

            var auth = TestApiFactory.CreateAuth(api, tempDir);
            var ex = await Assert.ThrowsAsync<ApiException>(() => auth.GetCurrentUserAsync());

            Assert.True(expiredCalled);
            Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
            Assert.Contains("Sesja wygasła", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoginAsync_On401_ShouldNotAttemptRefreshLoop()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            var refreshCalls = 0;
            api.TryRefreshSessionAsync = () =>
            {
                refreshCalls++;
                return Task.FromResult(true);
            };

            handler.EnqueueJson(HttpStatusCode.Unauthorized, """{ "message": "Invalid" }""");

            var auth = TestApiFactory.CreateAuth(api, tempDir);

            await Assert.ThrowsAsync<ApiException>(() => auth.LoginAsync("bad", "bad"));

            Assert.Equal(0, refreshCalls);
            Assert.Single(handler.Requests);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
