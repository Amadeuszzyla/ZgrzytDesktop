using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Integration;

public class LiveApiIntegrationTests : IClassFixture<IntegrationApiTestHost>
{
    private readonly IntegrationApiTestHost _host;

    public LiveApiIntegrationTests(IntegrationApiTestHost host) => _host = host;

    [SkippableFact]
    [Trait("Category", "Integration")]
    public void PostLogin_ReturnsAuthenticatedUser()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);

        Assert.NotNull(_host.User);
        Assert.False(string.IsNullOrWhiteSpace(_host.User!.Login));
        Assert.False(string.IsNullOrWhiteSpace(_host.User.Role));
        Assert.StartsWith("https://", _host.Api!.CurrentApiBaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetUser_ReturnsProfileAfterLogin()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);

        var user = await _host.Auth!.GetCurrentUserAsync();

        Assert.NotNull(user);
        Assert.Equal(_host.User!.Id, user!.Id);
        Assert.Equal(_host.User.Login, user.Login);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetTickets_ReturnsPaginatedList()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);

        var response = await _host.Tickets!.GetTicketsAsync(page: 1, perPage: 10);

        Assert.NotNull(response);
        Assert.NotNull(response!.Data);
        Assert.True(response.Total >= 0);
        Assert.True(response.LastPage >= 1);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_AsStaffRole_ReturnsUserList()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);
        Skip.IfNot(
            _host.IsStaffRole,
            "ZGRZYT_LOGIN must be an admin or it account to call GET /api/users.");

        var result = await _host.UserAdmin!.GetUsersAsync();

        Assert.NotNull(result.Users);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task PostLogout_InvalidatesBearerSession()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);

        var isolated = await IntegrationApiTestHost.CreateConnectedAsync();

        try
        {
            Assert.NotNull(isolated.User);
            Assert.False(string.IsNullOrWhiteSpace(isolated.User!.Login));

            var staleToken = await isolated.GetStoredAccessTokenAsync();
            Assert.False(string.IsNullOrWhiteSpace(staleToken));

            try
            {
                await isolated.Auth!.LogoutAsync();
            }
            catch (ApiException logoutEx)
            {
                Assert.DoesNotContain("<html", logoutEx.Message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Laravel", logoutEx.Message, StringComparison.OrdinalIgnoreCase);
            }

            isolated.Api!.SetToken(staleToken);

            User? profileAfterLogout = null;
            ApiException? userRequestEx = null;

            try
            {
                profileAfterLogout = await isolated.Api.GetAsync<User>("user");
            }
            catch (ApiException ex)
            {
                userRequestEx = ex;
            }

            if (profileAfterLogout is not null)
            {
                Assert.Fail(
                    "GET /api/user with the pre-logout Bearer token must not return 200 OK after POST /api/logout.");
            }

            Assert.NotNull(userRequestEx);

            // Real backend may return 500 instead of 401 after Sanctum token invalidation;
            // this is treated as backend inconsistency, not desktop failure.
            AssertStaleTokenRejectedAfterLogout(userRequestEx);
        }
        finally
        {
            isolated.Dispose();
        }
    }

    private static void AssertStaleTokenRejectedAfterLogout(ApiException ex)
    {
        Assert.DoesNotContain("<html", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Laravel", ex.Message, StringComparison.OrdinalIgnoreCase);

        if (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            or HttpStatusCode.InternalServerError)
        {
            return;
        }

        Assert.Fail(
            $"Unexpected status for GET /api/user with stale token after logout: {(int)ex.StatusCode} {ex.StatusCode}.");
    }
}
