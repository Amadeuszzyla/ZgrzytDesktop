using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class UserAdminServiceTests
{
    [Theory]
    [InlineData(UserAdminListFilter.All, "users")]
    [InlineData(UserAdminListFilter.Active, "active-users")]
    [InlineData(UserAdminListFilter.Inactive, "inactive-users")]
    [InlineData(UserAdminListFilter.Banned, "banned-users")]
    public void ResolveUsersListEndpoint_ShouldMatchOpenApiPaths(
        UserAdminListFilter filter,
        string expectedEndpoint)
    {
        Assert.Equal(expectedEndpoint, UserAdminService.ResolveUsersListEndpoint(filter));
    }

    [Theory]
    [InlineData(UserAdminListFilter.Active, "/api/active-users")]
    [InlineData(UserAdminListFilter.Inactive, "/api/inactive-users")]
    [InlineData(UserAdminListFilter.Banned, "/api/banned-users")]
    public async Task GetUsersAsync_ShouldCallCorrectListEndpoint(
        UserAdminListFilter filter,
        string expectedPathSuffix)
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "[]");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.GetUsersAsync(filter);

            var path = TestApiFactory.LastRequestPath(handler);
            Assert.NotNull(path);
            Assert.EndsWith(expectedPathSuffix, path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.NotNull(handler.Requests[0].Uri);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task BanUserAsync_ShouldPostToBanEndpoint()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "{}");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.BanUserAsync(7);

            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.EndsWith("/api/users/7/ban", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task ActivateUserAsync_ShouldPostToActivateEndpoint()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "{}");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.ActivateUserAsync(4);

            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.EndsWith("/api/users/4/activate", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldPostPasswordInBody()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "{}");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.UnbanUserAsync(9, "admin-secret");

            Assert.EndsWith("/api/users/9/unban", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            var body = TestApiFactory.LastRequestBody(handler);
            using var document = JsonDocument.Parse(body!);

            Assert.Equal("admin-secret", document.RootElement.GetProperty("password").GetString());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_ShouldDeserializeUsers()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                [
                  { "id": 1, "login": "jan", "email": "jan@test.pl", "role": "user", "active": true, "ban": false }
                ]
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var users = await service.GetUsersAsync(UserAdminListFilter.Active);

            Assert.NotNull(users);
            Assert.Single(users!);
            Assert.Equal("jan", users![0].Login);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
