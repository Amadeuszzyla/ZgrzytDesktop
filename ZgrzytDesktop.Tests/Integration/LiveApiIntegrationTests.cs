using System.Net;
using ZgrzytDesktop.Exceptions;
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
            await isolated.Auth!.LogoutAsync();

            var ex = await Assert.ThrowsAsync<ApiException>(() => isolated.Auth.GetCurrentUserAsync());

            Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        }
        finally
        {
            isolated.Dispose();
        }
    }
}
