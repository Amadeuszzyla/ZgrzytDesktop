using System.Text.Json;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Helpers;

public class ApiUserListParserTests
{
    [Fact]
    public void ParseUsers_ArrayEnvelope_ReturnsUsers()
    {
        using var document = JsonDocument.Parse("""
            [
              { "id": 1, "login": "it1", "role": "it", "active": true }
            ]
            """);

        var users = ApiUserListParser.ParseUsers(document.RootElement);

        Assert.Single(users);
        Assert.Equal("it1", users[0].Login);
    }

    [Fact]
    public void ParseUsers_PaginatedEnvelope_ReturnsDataItems()
    {
        using var document = JsonDocument.Parse("""
            {
              "current_page": 1,
              "data": [
                { "id": 2, "login": "admin1", "role": "admin", "active": true },
                { "id": 3, "login": "it2", "role": "it", "active": true }
              ],
              "total": 2
            }
            """);

        var users = ApiUserListParser.ParseUsers(document.RootElement);

        Assert.Equal(2, users.Count);
        Assert.Equal(AppRoles.Admin, users[0].Role);
    }
}
