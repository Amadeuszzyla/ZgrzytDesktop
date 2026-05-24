using System.Net;
using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
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

            var result = await service.GetUsersAsync(UserAdminListFilter.Active);

            Assert.Single(result.Users);
            Assert.Equal("jan", result.Users[0].Login);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_All_WhenApiReturnsPaginatedEnvelope_ShouldDeserializeData()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                {
                  "current_page": 1,
                  "data": [
                    { "id": 1, "login": "it1", "role": "it", "active": true, "ban": false },
                    { "id": 2, "login": "admin1", "role": "admin", "active": true, "ban": false }
                  ],
                  "total": 2
                }
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var result = await service.GetUsersAsync(UserAdminListFilter.All);

            Assert.Equal(2, result.Users.Count);
            Assert.Contains(result.Users, user => user.Login == "it1");
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_WhenActiveUsersReturns404_ShouldFallbackToUsersAndFilterLocally()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.NotFound, content: """{ "message": "Not Found" }""", mediaType: "application/json");
            handler.EnqueueJson(HttpStatusCode.OK, """
                [
                  { "id": 1, "login": "active-user", "active": true, "ban": false },
                  { "id": 2, "login": "inactive-user", "active": false, "ban": false }
                ]
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var result = await service.GetUsersAsync(UserAdminListFilter.Active);

            Assert.Single(result.Users);
            Assert.Equal("active-user", result.Users[0].Login);
            Assert.True(result.UsedLocalFilterFallback);
            Assert.Equal(UserAdminListInfoKind.LocalFilterFallback, result.InfoKind);
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("/api/users", handler.Requests[1].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_WhenInactiveUsersReturns404_ShouldFallbackToUsersAndFilterLocally()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.NotFound, content: """{ "message": "Not Found" }""", mediaType: "application/json");
            handler.EnqueueJson(HttpStatusCode.OK, """
                [
                  { "id": 1, "login": "active-user", "active": true, "ban": false },
                  { "id": 2, "login": "inactive-user", "active": false, "ban": false }
                ]
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var result = await service.GetUsersAsync(UserAdminListFilter.Inactive);

            Assert.Single(result.Users);
            Assert.Equal("inactive-user", result.Users[0].Login);
            Assert.True(result.UsedLocalFilterFallback);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_WhenBannedUsersReturns404_ShouldFallbackToUsers()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.NotFound, content: """{ "message": "Not Found" }""", mediaType: "application/json");
            handler.EnqueueJson(HttpStatusCode.OK, """
                [
                  { "id": 1, "login": "banned-user", "active": false, "ban": true },
                  { "id": 2, "login": "clean-user", "active": true, "ban": false }
                ]
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var result = await service.GetUsersAsync(UserAdminListFilter.Banned);

            Assert.Single(result.Users);
            Assert.Equal("banned-user", result.Users[0].Login);
            Assert.True(result.UsedLocalFilterFallback);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_WhenBannedFallbackAndBanFieldMissing_ShouldReturnInformationalMessage()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.NotFound, content: """{ "message": "Not Found" }""", mediaType: "application/json");
            handler.EnqueueJson(HttpStatusCode.OK, """
                [
                  { "id": 1, "login": "jan", "active": true, "ban": false },
                  { "id": 2, "login": "adam", "active": false, "ban": false }
                ]
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var result = await service.GetUsersAsync(UserAdminListFilter.Banned);

            Assert.Empty(result.Users);
            Assert.Equal(UserAdminListInfoKind.BannedListNotSupported, result.InfoKind);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task BanUserAsync_WhenEndpointReturns404_ShouldThrowFriendlyMessage()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.NotFound, content: "<html>404</html>", mediaType: "text/html");
            var service = TestApiFactory.CreateUserAdmin(api);

            var ex = await Assert.ThrowsAsync<ApiException>(() => service.BanUserAsync(3));

            Assert.Equal(AppStrings.Get("Admin_ActionNotSupported"), ex.Message);
            Assert.DoesNotContain("<html", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Theory]
    [InlineData(UserAdminListFilter.Active, true, false, true)]
    [InlineData(UserAdminListFilter.Inactive, false, false, true)]
    [InlineData(UserAdminListFilter.Inactive, false, true, false)]
    [InlineData(UserAdminListFilter.Banned, true, true, true)]
    public void FilterUsersLocally_ShouldApplyExpectedRules(
        UserAdminListFilter filter,
        bool active,
        bool ban,
        bool expectedIncluded)
    {
        var users = new List<User>
        {
            new() { Id = 1, Login = "target", Active = active, Ban = ban },
            new() { Id = 2, Login = "other", Active = true, Ban = false }
        };

        var filtered = UserAdminService.FilterUsersLocally(users, filter);

        if (expectedIncluded)
            Assert.Contains(filtered, user => user.Login == "target");
        else
            Assert.DoesNotContain(filtered, user => user.Login == "target");
    }

    [Fact]
    public async Task GetUsersAsync_FilterAll_ShouldCallOnlyUsersEndpoint()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "[]");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.GetUsersAsync(UserAdminListFilter.All);

            Assert.Single(handler.Requests);
            Assert.EndsWith("/api/users", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetUsersAsync_WhenActiveUsersReturns403_ShouldNotFallbackToUsers()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.Enqueue(statusCode: HttpStatusCode.Forbidden, content: """{ "message": "Forbidden" }""", mediaType: "application/json");
            var service = TestApiFactory.CreateUserAdmin(api);

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                service.GetUsersAsync(UserAdminListFilter.Active));

            Assert.Equal(AppStrings.Get("Admin_ListForbidden"), ex.Message);
            Assert.Single(handler.Requests);
            Assert.EndsWith("/api/active-users", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
