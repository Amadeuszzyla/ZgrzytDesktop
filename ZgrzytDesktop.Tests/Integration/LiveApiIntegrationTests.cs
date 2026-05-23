using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Integration;

/// <summary>
/// Live API contract tests. Require ZGRZYT_API_URL, ZGRZYT_LOGIN, ZGRZYT_PASSWORD — otherwise skipped.
/// Read-only against production data; logout test uses an isolated session only.
/// </summary>
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

        LiveApiTestHelpers.AssertPaginatedTickets(response);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetUsers_AsStaffRole_ReturnsUserList()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);
        Skip.IfNot(_host.IsStaffRole, LiveApiTestHelpers.StaffRoleSkipReason);

        var result = await _host.UserAdmin!.GetUsersAsync();

        Assert.NotNull(result.Users);
    }

    /// <summary>
    /// Verifies Bearer invalidation after POST /api/logout.
    /// Known backend: GET /api/user with a revoked token may return 500 instead of 401 on Render/Sanctum;
    /// POST /api/logout usually succeeds — the 500 appears on the follow-up authenticated request.
    /// Security requirement: stale token must not return 200 OK with a user profile.
    /// </summary>
    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task PostLogout_InvalidatesBearerSession()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);

        var isolated = await IntegrationApiTestHost.CreateConnectedAsync();

        try
        {
            Assert.NotNull(isolated.User);

            var profileBeforeLogout = await isolated.Auth!.GetCurrentUserAsync();
            Assert.NotNull(profileBeforeLogout);

            var staleToken = await isolated.GetStoredAccessTokenAsync();
            Assert.False(string.IsNullOrWhiteSpace(staleToken));

            ApiException? logoutPostEx = null;

            try
            {
                await isolated.Auth.LogoutAsync();
            }
            catch (ApiException ex)
            {
                logoutPostEx = ex;
            }

            LiveApiTestHelpers.AssertLogoutPostDoesNotLeakHtml(logoutPostEx);

            // LogoutAsync clears the in-memory token; restore the pre-logout token to probe invalidation.
            isolated.Api!.SetToken(staleToken);

            User? profileAfterLogout = null;
            ApiException? staleUserRequestEx = null;

            try
            {
                profileAfterLogout = await isolated.Api.GetAsync<User>("user");
            }
            catch (ApiException ex)
            {
                staleUserRequestEx = ex;
            }

            if (profileAfterLogout is not null)
            {
                Assert.Fail(
                    "GET /api/user with the pre-logout Bearer token returned 200 OK after POST /api/logout. " +
                    "The token must be invalidated (expected 401/403 or known backend 500, not a live session).");
            }

            Assert.NotNull(staleUserRequestEx);
            LiveApiTestHelpers.AssertStaleTokenDoesNotAuthenticate(
                staleUserRequestEx,
                "GET /api/user after POST /api/logout");
        }
        finally
        {
            isolated.Dispose();
        }
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetActiveTickets_AsStaffRole_ReturnsPaginatedList()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);
        Skip.IfNot(_host.IsStaffRole, LiveApiTestHelpers.StaffRoleSkipReason);

        var status = await _host.GetEndpointStatusAsync("active-tickets?page=1&per_page=10");
        LiveApiTestHelpers.AssertStaffListStatus(status, "active-tickets", allowNotFoundFallback: false);

        var response = await _host.Tickets!.GetActiveTicketsAsync(page: 1, perPage: 10);
        LiveApiTestHelpers.AssertPaginatedTickets(response);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetUnassignedTickets_AsStaffRole_ReturnsPaginatedList()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);
        Skip.IfNot(_host.IsStaffRole, LiveApiTestHelpers.StaffRoleSkipReason);

        var status = await _host.GetEndpointStatusAsync("unassigned-tickets?page=1&per_page=10");
        LiveApiTestHelpers.AssertStaffListStatus(status, "unassigned-tickets", allowNotFoundFallback: false);

        var response = await _host.Tickets!.GetUnassignedTicketsAsync(page: 1, perPage: 10);
        LiveApiTestHelpers.AssertPaginatedTickets(response);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetStaffUserListEndpoints_ReturnsOkOrDocumentsNotFound()
    {
        Skip.IfNot(_host.IsConfigured, IntegrationTestEnvironment.SkipReason);
        Skip.IfNot(_host.IsStaffRole, LiveApiTestHelpers.StaffRoleSkipReason);

        LiveApiTestHelpers.AssertStaffListStatus(
            await _host.GetEndpointStatusAsync("users"),
            "users",
            allowNotFoundFallback: false);

        LiveApiTestHelpers.AssertStaffListStatus(
            await _host.GetEndpointStatusAsync("active-users"),
            "active-users",
            allowNotFoundFallback: true);

        LiveApiTestHelpers.AssertStaffListStatus(
            await _host.GetEndpointStatusAsync("inactive-users"),
            "inactive-users",
            allowNotFoundFallback: true);

        var bannedStatus = await _host.GetEndpointStatusAsync("banned-users");
        LiveApiTestHelpers.AssertStaffListStatus(
            bannedStatus,
            "banned-users",
            allowNotFoundFallback: true);

        var allUsers = await _host.UserAdmin!.GetUsersAsync();
        Assert.NotNull(allUsers.Users);

        var activeUsers = await _host.UserAdmin.GetActiveUsersAsync();
        Assert.NotNull(activeUsers.Users);

        var inactiveUsers = await _host.UserAdmin.GetInactiveUsersAsync();
        Assert.NotNull(inactiveUsers.Users);

        var bannedUsers = await _host.UserAdmin.GetBannedUsersAsync();
        Assert.NotNull(bannedUsers.Users);
    }
}
